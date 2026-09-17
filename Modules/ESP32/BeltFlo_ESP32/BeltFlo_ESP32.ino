
#include "src/ESP2SOTA_RC/index_html.h"
#include "src/ESP2SOTA_RC/ESP2SOTA_RC.h"	// modified from https://github.com/pangodream/ESP2SOTA

#include <WiFi.h>
#include <ESPmDNS.h>
#include <WebServer.h>
#include <DNSServer.h>

#include <WiFiUdp.h>
#include <WiFiClient.h>
#include <EEPROM.h>
#include <Wire.h>

#include <SPI.h>
#include <Ethernet_Generic.h>	// W5500 wired ethernet (same library as AOG_RC)
#include <EthernetUdp.h>

#include "driver/twai.h"

// BeltFlo module, board: DOIT ESP32 DEVKIT V1
#define InoDescription "BeltFlo_ESP32"
#define InoID 16096         // firmware version — update with every build (DDMMY format)
#define StructVersion 1     // EEPROM layout version — increment ONLY when ModuleConfig fields change

// Comm modes
const uint8_t CommModeWifi = 0;
const uint8_t CommModeCan = 1;
const uint8_t CommModeEth = 2;

const uint8_t NC = 0xFF;		// Pin not connected
const uint8_t ModStringLengths = 15;
const uint16_t EEPROM_SIZE = 512;

// ISR forward declarations — the Arduino sketch preprocessor does not
// auto-generate prototypes for functions with IRAM_ATTR.
void IRAM_ATTR onBeltPulse();

// ── Conveyor scale ────────────────────────────────────────────────────────
// Declared here rather than in Scale.ino because the .ino files are
// concatenated alphabetically: Comm.ino is compiled first and reads this state
// to build its packets. Functions are auto-prototyped across files, variables
// are not. The logic that maintains all of this lives in Scale.ino.
//
// Settings come from the app. The defaults are deliberately inert — nothing is
// trusted until a settings message arrives, and the Tared flag stays clear
// until one does, so the app shows "Zero" rather than a plausible wrong weight.
int32_t  ScaleZeroCounts      = 0;
float    ScaleSpanLbPerCnt    = 1.0f;
uint16_t ScaleSectionLenX10   = 360;    // 36.0 in
uint16_t ScaleInPerPulseX1000 = 1000;   // 1.000 in
uint8_t  ScaleBeltStopX10     = 20;     // 2.0 s
bool     ScaleSettingsSeen    = false;  // a valid settings message has arrived at least once
uint32_t ScaleSettingsMs      = 0;      // millis() of the last valid settings message

// Flags bit 3 tells the app the link works both ways. Four seconds covers two
// missed 2 s heartbeats without flickering.
const uint32_t SettingsFreshMs = 4000;

volatile uint32_t BeltPulses     = 0;   // raw ISR count, wraps
volatile uint32_t LastBeltEdgeUs = 0;

int32_t  ScaleRaw        = 0;       // last converter reading
double   ScaleLb         = 0;       // load on the section after zero and span
bool     ScaleFound      = false;   // the converter answered at boot
bool     ScaleOverload   = false;
uint32_t ScaleLastReadMs = 0;       // millis() of the last successful conversion

uint32_t CumPulses       = 0;       // sent to the app, wraps
double   CumPounds       = 0;       // accumulated in double, sent as tenths
uint32_t LastIntegrated  = 0;       // pulse count the integration last consumed

struct ModuleConfig
{
	uint8_t ID = 0;
	char APname[ModStringLengths] = "BeltFlo_ESP32";
	char APpassword[ModStringLengths] = "";
	bool WifiModeUseStation = false;				// false - AP mode, true - AP + Station
	char SSID[ModStringLengths] = "Tractor";		// name of network ESP32 connects to
	char Password[ModStringLengths] = "111222333";
	uint8_t CommMode = CommModeWifi;	// 0 = WiFi UDP, 1 = CAN bus, 2 = Ethernet UDP
	uint8_t CanTxPin = 14;			// TWAI TX → MCP2562 TXD
	uint8_t CanRxPin = 27;			// TWAI RX ← MCP2562 RXD
	uint8_t EthIP0 = 192;			// Ethernet subnet — module IP is EthIP0.EthIP1.EthIP2.(50+ID)
	uint8_t EthIP1 = 168;
	uint8_t EthIP2 = 1;
	uint8_t StaChannelCache = 0;	// channel the station network was last found on; 0 = unknown
	uint8_t BeltPin = 34;			// belt travel proximity sensor, one pulse per target
};
ModuleConfig MDL;

// ethernet (W5500 on VSPI: SCK 18, MISO 19, MOSI 23, SS 5 — same wiring as AOG_RC ESP32)
const uint8_t W5500_SS = 5;
EthernetUDP UDP_Ethernet;
bool EthChipFound = false;
IPAddress Ethernet_DestinationIP;

// wifi
WiFiUDP UDP_Wifi;
IPAddress Wifi_DestinationIP(192, 168, 100, 255);
WiFiClient client;
WebServer server(80);
DNSServer dnsServer;
const byte AP_DNS_PORT = 53;
// BeltFlo's own ports, so a grain YieldFlo and a conveyor BeltFlo can share a
// network without reading each other's packets — and because a running YieldFlo
// holds 30100. Module → PC on 30300; PC → module settings arrive on 30400.
const uint16_t ListeningPort  = 30400;
const uint16_t ModuleSendPort = 30300;	// PC receive port

// WiFi station connection management lives in Wifi.ino — the event handlers,
// the paced retry, and the policy behind both.

const uint16_t SendTimePK1 = 200;  // ms = 5 Hz  (conveyor packet)
uint32_t       SendLastPK1 = SendTimePK1;

// Why the module last restarted, normalised to the same codes on both
// platforms so the app does not need to know which one it is talking to:
//   0 unknown  1 power-on  2 reset pin  3 software  4 watchdog
//   5 brownout  6 panic/fault  7 other
// Captured once at boot because the hardware flags are cleared after reading.
uint8_t ResetReasonCode = 0;

void setup()
{
	DoSetup();
}

void loop()
{
	dnsServer.processNextRequest();
	server.handleClient();
	ReceiveComm();
	ServiceWifiStation();   // paced station reconnect — see Wifi.ino
	// The scale is serviced every loop rather than on a timer: the converter
	// runs at 80 SPS and each conversion is folded against the belt travel since
	// the last one, so waiting would integrate a stale weight over new travel.
	ScaleService();
	if (MDL.CommMode == CommModeCan)
	{
		CheckCanBus();
		SendCAN();
	}
	else
	{
		SendUdp();   // WiFi or Ethernet, per MDL.CommMode
	}
	//Blink();
}

bool GoodCRC(byte Data[], byte Length)
{
	byte ck = CRC(Data, Length - 1, 0);
	bool Result = (ck == Data[Length - 1]);
	return Result;
}

byte CRC(byte Chk[], byte Length, byte Start)
{
	byte Result = 0;
	for (int i = Start; i < Length; i++)
	{
		Result += Chk[i];
	}
	return Result;
}

//bool State = false;
//uint32_t LastBlink;
//uint32_t LastLoop;
//byte ReadReset;
//uint32_t MaxLoopTime;
//double debug1;
//double debug2;
//
//// max loop about 2500, 18/Sep/2025
//void Blink()
//{
//	if (millis() - LastBlink > 1000)
//	{
//		LastBlink = millis();
//		State = !State;
//
//		Serial.print(MaxLoopTime);
//
//		Serial.print(", ");
//		Serial.print(debug1);
//
//		Serial.print(", ");
//		Serial.print(debug2);
//
//		//Serial.print(", ");
//		//Serial.print(WifiMasterOn);
//
//		Serial.print(", ");
//		Serial.print(Sensor[0].TotalPulses);
//
//		Serial.println("");
//
//		if (ReadReset++ > 5)
//		{
//			ReadReset = 0;
//			MaxLoopTime = 0;
//		}
//	}
//	if (micros() - LastLoop > MaxLoopTime) MaxLoopTime = micros() - LastLoop;
//	LastLoop = micros();
//}

