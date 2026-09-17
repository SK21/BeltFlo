
// ── CAN bus health monitoring ─────────────────────────────────────────────
static uint32_t LastBusOffMs = 0;
static uint8_t  BusOffCount = 0;

// Called every loop() when CAN mode is active.
// Handles Bus Off recovery and falls back to WiFi after repeated failures.
void CheckCanBus()
{
	twai_status_info_t st;
	if (twai_get_status_info(&st) != ESP_OK) return;

	if (st.state == TWAI_STATE_BUS_OFF)
	{
		if (millis() - LastBusOffMs > 3000)
		{
			LastBusOffMs = millis();
			BusOffCount++;
			Serial.print("CAN Bus Off. Recovery attempt ");
			Serial.println(BusOffCount);
			twai_initiate_recovery();   // waits for 128 × 11 recessive bits, then auto-starts
		}

		if (BusOffCount > 5)
		{
			Serial.println("CAN Bus Off: persistent. Falling back to WiFi.");
			MDL.CommMode = CommModeWifi;     // session only — EEPROM unchanged, reverts on restart
			BusOffCount = 0;
			SendLastPK1 = 0;         // send WiFi immediately on next loop
		}
	}
	else if (st.state == TWAI_STATE_STOPPED)
	{
		twai_start();
	}

	// Status line — the controller's own account of what is wrong:
	//   msgs_to_tx growing + no errors = RX line stuck dominant (bus "always busy")
	//   tx_error_counter pinned high   = frames going out but never ACKed
	//   bus_error_count climbing       = bit/form errors (TXD/RXD wiring)
	// Checked every 5 s but printed only when something changed, so a healthy
	// steady bus is silent and a developing fault still logs every 5 s.
	static uint32_t LastStatusMs = 0;
	static twai_status_info_t PrevSt;
	static bool StatusPrinted = false;
	if (millis() - LastStatusMs > 5000)
	{
		LastStatusMs = millis();

		if (StatusPrinted
			&& st.state == PrevSt.state
			&& st.msgs_to_tx == PrevSt.msgs_to_tx
			&& st.tx_error_counter == PrevSt.tx_error_counter
			&& st.rx_error_counter == PrevSt.rx_error_counter
			&& st.tx_failed_count == PrevSt.tx_failed_count
			&& st.bus_error_count == PrevSt.bus_error_count
			&& st.arb_lost_count == PrevSt.arb_lost_count) return;

		PrevSt = st;
		StatusPrinted = true;

		Serial.print("CAN state=");
		Serial.print((int)st.state);
		Serial.print(" txq=");
		Serial.print(st.msgs_to_tx);
		Serial.print(" TEC=");
		Serial.print(st.tx_error_counter);
		Serial.print(" REC=");
		Serial.print(st.rx_error_counter);
		Serial.print(" txFailed=");
		Serial.print(st.tx_failed_count);
		Serial.print(" busErr=");
		Serial.print(st.bus_error_count);
		Serial.print(" arbLost=");
		Serial.println(st.arb_lost_count);
	}
}

// ── Conveyor packet body ─────────────────────────────────────────────────
// The flags byte both transports carry:
//   bit0 ScaleOK          the converter is answering and inside its range
//   bit1 BeltRunning      pulses still arriving
//   bit2 Tared            the module holds a zero from the app
//   bit3 ReceivingFromPC  a valid settings message within the last 4 s
//   bit4 Overload         cells at their rated limit
static byte ConveyorFlags()
{
	byte f = 0;
	if (ScaleIsOK())        f |= 0x01;
	if (BeltIsRunning())    f |= 0x02;
	if (ScaleIsTared())     f |= 0x04;
	if (SettingsAreFresh()) f |= 0x08;
	if (ScaleOverload)      f |= 0x10;
	return f;
}

void SendCAN()
{
	SendCANPK1();
}

// ── CAN bus send (5 Hz) ──────────────────────────────────────────────────
// Counters on 0x18FF02F8, status on 0x18FF03F8 — extended, priority 6,
// ProprietaryB, source address 0xF8 (the module). Two frames because eight
// bytes will not hold both counters and the live weight; the app applies the
// status frame first so a counter row carries this cycle's flags.
void SendCANPK1()
{
	if (millis() - SendLastPK1 < SendTimePK1) return;

	// Skip transmit only if controller is stopped. Do NOT gate on tx_error_counter:
	// with no ACKing node on the bus (app not open yet) TEC rises to error-passive,
	// and TEC only falls on successful TX — a TEC guard here would block transmit
	// forever. ACK errors at error-passive cannot reach Bus Off (CAN spec), so
	// transmitting in that state is safe; CheckCanBus handles recovery if it stops.
	twai_status_info_t st;
	if (twai_get_status_info(&st) != ESP_OK)           return;
	if (st.state != TWAI_STATE_RUNNING)                return;

	SendLastPK1 = millis();

	uint32_t lbX10  = CumPoundsX10();
	uint32_t pulses = CumPulses;
	int16_t  liveX10 = ScaleLbX10();
	int32_t  raw    = ScaleRaw;

	// Status first: the app applies whichever arrives first, and a counter row
	// logged against the previous cycle's flags is the one confusing case.
	twai_message_t st2;
	memset(&st2, 0, sizeof(st2));
	st2.extd = 1;
	st2.identifier = 0x18FF03F8;
	st2.data_length_code = 8;
	st2.data[0] = ConveyorFlags();
	st2.data[1] = (byte)(liveX10 & 0xFF);
	st2.data[2] = (byte)((liveX10 >> 8) & 0xFF);
	st2.data[3] = (byte)(raw & 0xFF);
	st2.data[4] = (byte)((raw >> 8) & 0xFF);
	st2.data[5] = (byte)((raw >> 16) & 0xFF);
	st2.data[6] = (byte)((raw >> 24) & 0xFF);
	st2.data[7] = 0;                            // reserved
	twai_transmit(&st2, pdMS_TO_TICKS(10));

	twai_message_t ct;
	memset(&ct, 0, sizeof(ct));
	ct.extd = 1;
	ct.identifier = 0x18FF02F8;
	ct.data_length_code = 8;
	ct.data[0] = (byte)(lbX10 & 0xFF);
	ct.data[1] = (byte)((lbX10 >> 8) & 0xFF);
	ct.data[2] = (byte)((lbX10 >> 16) & 0xFF);
	ct.data[3] = (byte)((lbX10 >> 24) & 0xFF);
	ct.data[4] = (byte)(pulses & 0xFF);
	ct.data[5] = (byte)((pulses >> 8) & 0xFF);
	ct.data[6] = (byte)((pulses >> 16) & 0xFF);
	ct.data[7] = (byte)((pulses >> 24) & 0xFF);
	twai_transmit(&ct, pdMS_TO_TICKS(10));
}

void SendUdp()
{
	SendUdpPK1();
}

// Send one packet on the configured UDP transport (WiFi or W5500 ethernet)
void UdpSend(byte pkt[], int len)
{
	if (MDL.CommMode == CommModeEth)
	{
		if (!EthChipFound || Ethernet.linkStatus() != LinkON) return;
		UDP_Ethernet.beginPacket(Ethernet_DestinationIP, ModuleSendPort);
		UDP_Ethernet.write(pkt, len);
		UDP_Ethernet.endPacket();
	}
	else
	{
		UDP_Wifi.beginPacket(Wifi_DestinationIP, ModuleSendPort);
		UDP_Wifi.write(pkt, len);
		UDP_Wifi.endPacket();
	}
}

// ── UDP send (5 Hz) ──────────────────────────────────────────────────────
// Conveyor packet, PGN 40010, 19 bytes. Counters and status travel together
// here, unlike CAN, so the app never sees one without the other.
//   [0-1]   PGN 40010 LE (0x4A 0x9C)
//   [2]     flags
//   [3-6]   cum_pounds_x10  uint32 LE, wraps
//   [7-10]  cum_pulses      uint32 LE, wraps
//   [11-12] scale_lb_x10    int16 LE, live load on the section after zero
//   [13-16] scale_raw       int32 LE, raw converter counts
//   [17]    reserved
//   [18]    CRC8, byte sum of everything before it
void SendUdpPK1()
{
	if (millis() - SendLastPK1 < SendTimePK1) return;
	SendLastPK1 = millis();

	uint32_t lbX10   = CumPoundsX10();
	uint32_t pulses  = CumPulses;
	int16_t  liveX10 = ScaleLbX10();
	int32_t  raw     = ScaleRaw;

	byte pkt[19];
	pkt[0]  = 0x4A;		// PGN 40010 low byte
	pkt[1]  = 0x9C;		// PGN 40010 high byte
	pkt[2]  = ConveyorFlags();
	pkt[3]  = (byte)(lbX10 & 0xFF);
	pkt[4]  = (byte)((lbX10 >> 8) & 0xFF);
	pkt[5]  = (byte)((lbX10 >> 16) & 0xFF);
	pkt[6]  = (byte)((lbX10 >> 24) & 0xFF);
	pkt[7]  = (byte)(pulses & 0xFF);
	pkt[8]  = (byte)((pulses >> 8) & 0xFF);
	pkt[9]  = (byte)((pulses >> 16) & 0xFF);
	pkt[10] = (byte)((pulses >> 24) & 0xFF);
	pkt[11] = (byte)(liveX10 & 0xFF);
	pkt[12] = (byte)((liveX10 >> 8) & 0xFF);
	pkt[13] = (byte)(raw & 0xFF);
	pkt[14] = (byte)((raw >> 8) & 0xFF);
	pkt[15] = (byte)((raw >> 16) & 0xFF);
	pkt[16] = (byte)((raw >> 24) & 0xFF);
	pkt[17] = 0;		// reserved
	pkt[18] = CRC(pkt, 18, 0);

	UdpSend(pkt, 19);
}

// ── Settings from the PC (PGN 40011) ─────────────────────────────────────
// 18 bytes:
//   [0-1]   PGN 40011 LE (0x4B 0x9C)
//   [2-14]  the 13-byte settings block
//   [15-16] CRC-16/CCITT-FALSE of the block, LE
//   [17]    CRC8, byte sum of everything before it
//
// Both checks must pass. The CRC8 catches a truncated or mis-framed packet the
// way every other packet here does; the CRC-16 is what the app relies on to
// know a damaged block was not applied. Arriving at all is what sets flags
// bit 3, so the app can see the link works in both directions.
static void HandleSettingsPacket(const byte pkt[], int len)
{
	if (len < 18) return;
	if (pkt[0] != 0x4B || pkt[1] != 0x9C) return;
	if (CRC((byte*)pkt, 17, 0) != pkt[17]) return;

	byte block[13];
	memcpy(block, pkt + 2, 13);

	uint16_t want = (uint16_t)pkt[15] | ((uint16_t)pkt[16] << 8);
	if (Crc16CCITT(block, 13) != want) return;

	ApplyScaleSettings(block);
}

// ── Receive ──────────────────────────────────────────────────────────────
void ReceiveComm()
{
	byte buf[64];

	int sz = UDP_Wifi.parsePacket();
	while (sz > 0)
	{
		int n = UDP_Wifi.read(buf, sizeof(buf));
		if (n > 0) HandleSettingsPacket(buf, n);
		sz = UDP_Wifi.parsePacket();
	}

	if (EthChipFound)
	{
		sz = UDP_Ethernet.parsePacket();
		while (sz > 0)
		{
			int n = UDP_Ethernet.read(buf, sizeof(buf));
			if (n > 0) HandleSettingsPacket(buf, n);
			sz = UDP_Ethernet.parsePacket();
		}
	}

	if (MDL.CommMode == CommModeCan) ReceiveCAN();
}

// ── Settings over CAN ────────────────────────────────────────────────────
// The block does not fit one frame, so the PC sends two:
//   0x18FF04F9  block bytes 0-7
//   0x18FF05F9  block bytes 8-12, then the CRC-16 LE, then a spare byte
// Held until both have arrived and the CRC-16 agrees. A half pair from an
// interrupted send is simply overwritten by the next one.
static byte CanSettingsBlock[13];
static bool CanFrameASeen = false;

void ReceiveCAN()
{
	twai_message_t msg;
	while (twai_receive(&msg, 0) == ESP_OK)
	{
		if (!msg.extd) continue;

		if (msg.identifier == 0x18FF04F9 && msg.data_length_code >= 8)
		{
			memcpy(CanSettingsBlock, msg.data, 8);
			CanFrameASeen = true;
		}
		else if (msg.identifier == 0x18FF05F9 && msg.data_length_code >= 7)
		{
			if (!CanFrameASeen) continue;
			memcpy(CanSettingsBlock + 8, msg.data, 5);

			uint16_t want = (uint16_t)msg.data[5] | ((uint16_t)msg.data[6] << 8);
			if (Crc16CCITT(CanSettingsBlock, 13) == want)
				ApplyScaleSettings(CanSettingsBlock);

			CanFrameASeen = false;
		}
	}
}
