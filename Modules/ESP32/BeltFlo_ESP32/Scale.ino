
// ── Conveyor scale ────────────────────────────────────────────────────────
//
// The module weighs, the app records. Weight on the weighed section is read
// from an NAU7802 24-bit bridge converter; belt travel is counted as pulses
// from a proximity sensor on a belt roller. Mass delivered is the product:
//
//     pounds = load_on_section × (belt_travel ÷ section_length)
//
// which is why the app pushes the section length and the belt travel per pulse
// down here rather than doing the sum itself — integrating at the source means
// a dropped packet costs nothing, because what is sent is a cumulative counter
// the app differences.
//
// Everything sent is cumulative and allowed to wrap; the app detects a counter
// reset and never loses a reading to a lost packet.

// NAU7802 — I2C address is fixed. Register numbers and bit positions are from
// the datasheet (rev 1.7), sections 10 and 11.
const uint8_t NAU_ADDR       = 0x2A;
const uint8_t NAU_PU_CTRL    = 0x00;   // AVDDS OSCS CR CS PUR PUA PUD RR
const uint8_t NAU_CTRL1      = 0x01;   // CRP DRDY_SEL VLDO[2:0] GAINS[2:0]
const uint8_t NAU_CTRL2      = 0x02;   // CHS CRS[2:0] CAL_ERR CALS CALMOD[1:0]
const uint8_t NAU_ADCO_B2    = 0x12;   // 24-bit result, MSB first at this address
const uint8_t NAU_REVISION   = 0x1F;

// PU_CTRL bits
const uint8_t PU_RR    = 0x01;
const uint8_t PU_PUD   = 0x02;
const uint8_t PU_PUA   = 0x04;
const uint8_t PU_PUR   = 0x08;   // read-only: powered up and ready
const uint8_t PU_CS    = 0x10;
const uint8_t PU_CR    = 0x20;   // read-only: a conversion is ready
const uint8_t PU_AVDDS = 0x80;

// CTRL2 bits
const uint8_t C2_CALS    = 0x04;
const uint8_t C2_CAL_ERR = 0x08;

// A 24-bit two's-complement converter saturates here. Treated as an overload
// rather than a reading: past this the cells are at their rated limit and the
// number means nothing.
const int32_t ScaleFullScale  = 8388607L;
const int32_t ScaleOverloadAt = 8200000L;   // ~98% of full scale

// No conversion within this long and the converter is considered dead. Three
// times the slowest configured rate, so a single missed sample is not a fault.
const uint32_t ScaleStaleMs = 1000;

// The settings and live state this file works on are declared in the main
// sketch. Arduino concatenates the .ino files alphabetically, so Comm.ino is
// compiled before this one and would not see them declared here — functions are
// auto-prototyped across files, variables are not.

// Belt pulses are a proximity sensor on a roller: one edge per target, no
// direction. Debounced only lightly — a fast belt on a many-target roller can
// pulse into the low kHz, far above the elevator RPM sensor this replaces.
void IRAM_ATTR onBeltPulse()
{
	uint32_t now = micros();
	if (now - LastBeltEdgeUs < 200) return;   // 200 µs = 5 kHz ceiling
	LastBeltEdgeUs = now;
	BeltPulses++;
}

// ── NAU7802 register access ───────────────────────────────────────────────

static bool NauWrite(uint8_t reg, uint8_t value)
{
	Wire.beginTransmission(NAU_ADDR);
	Wire.write(reg);
	Wire.write(value);
	return Wire.endTransmission() == 0;
}

static bool NauRead(uint8_t reg, uint8_t* value)
{
	Wire.beginTransmission(NAU_ADDR);
	Wire.write(reg);
	if (Wire.endTransmission() != 0) return false;
	if (Wire.requestFrom((int)NAU_ADDR, 1) != 1) return false;
	*value = Wire.read();
	return true;
}

// Reads the 24-bit conversion result and sign-extends it into an int32.
static bool NauReadAdc(int32_t* result)
{
	Wire.beginTransmission(NAU_ADDR);
	Wire.write(NAU_ADCO_B2);
	if (Wire.endTransmission() != 0) return false;
	if (Wire.requestFrom((int)NAU_ADDR, 3) != 3) return false;

	uint32_t v = (uint32_t)Wire.read() << 16;
	v |= (uint32_t)Wire.read() << 8;
	v |= (uint32_t)Wire.read();

	// Two's complement across 24 bits.
	*result = (v & 0x800000) ? (int32_t)(v | 0xFF000000) : (int32_t)v;
	return true;
}

// ── Setup ─────────────────────────────────────────────────────────────────
// Returns true when the converter answers and calibrates. Wire.begin() has
// already run in DoSetup().
bool ScaleSetup()
{
	ScaleFound = false;

	uint8_t rev;
	if (!NauRead(NAU_REVISION, &rev)) return false;

	// Register reset: RR is a level trigger, so it is raised and lowered.
	if (!NauWrite(NAU_PU_CTRL, PU_RR)) return false;
	delay(1);
	if (!NauWrite(NAU_PU_CTRL, 0)) return false;

	// Power up the digital side, then wait for it to report ready.
	if (!NauWrite(NAU_PU_CTRL, PU_PUD)) return false;
	uint32_t start = millis();
	uint8_t pu = 0;
	do
	{
		if (!NauRead(NAU_PU_CTRL, &pu)) return false;
		if (pu & PU_PUR) break;
		delay(1);
	} while (millis() - start < 200);
	if (!(pu & PU_PUR)) return false;

	// Analog on, and run the internal LDO rather than an external AVDD.
	if (!NauWrite(NAU_PU_CTRL, PU_PUD | PU_PUA | PU_AVDDS)) return false;

	// CTRL1: LDO 3.3 V (VLDO = 100), gain ×128 (GAINS = 111). A load cell at
	// 2 mV/V on a 3.3 V bridge swings about ±6.6 mV, which ×128 fills the
	// converter's input range without clipping.
	if (!NauWrite(NAU_CTRL1, (0x04 << 3) | 0x07)) return false;

	// CTRL2: 80 samples/s (CRS = 011). Fast enough that belt travel between
	// samples is short even on a quick belt, slow enough to stay quiet.
	if (!NauWrite(NAU_CTRL2, (0x03 << 4))) return false;

	delay(10);

	// Internal offset calibration (CALMOD = 00). Zeroes the converter's own
	// offset, not the belt's — the belt zero is the app's, sent as ZeroCounts.
	if (!NauWrite(NAU_CTRL2, (0x03 << 4) | C2_CALS)) return false;
	start = millis();
	uint8_t c2 = 0;
	do
	{
		if (!NauRead(NAU_CTRL2, &c2)) return false;
		if (!(c2 & C2_CALS)) break;
		delay(1);
	} while (millis() - start < 1000);
	if (c2 & C2_CALS)    return false;   // never finished
	if (c2 & C2_CAL_ERR) return false;   // finished badly

	// Start converting.
	if (!NauWrite(NAU_PU_CTRL, PU_PUD | PU_PUA | PU_AVDDS | PU_CS)) return false;

	ScaleFound = true;
	ScaleLastReadMs = millis();
	return true;
}

// ── Service ───────────────────────────────────────────────────────────────
// Called every loop. Takes a conversion when one is ready and folds the weight
// into the cumulative pounds against the belt travel since the last fold.
void ScaleService()
{
	if (!ScaleFound) return;

	uint8_t pu;
	if (!NauRead(NAU_PU_CTRL, &pu)) return;
	if (!(pu & PU_CR)) return;            // no new conversion yet

	int32_t raw;
	if (!NauReadAdc(&raw)) return;

	ScaleRaw = raw;
	ScaleLastReadMs = millis();
	ScaleOverload = (raw > ScaleOverloadAt) || (raw < -ScaleOverloadAt);

	// Load on the weighed section, in pounds.
	ScaleLb = (double)(raw - ScaleZeroCounts) * (double)ScaleSpanLbPerCnt;

	// Belt travel since the last fold. Taken here rather than in the ISR so the
	// weight and the travel belong to the same instant.
	noInterrupts();
	uint32_t pulsesNow = BeltPulses;
	interrupts();

	uint32_t delta = pulsesNow - LastIntegrated;   // unsigned: wraps correctly
	LastIntegrated = pulsesNow;
	CumPulses += delta;

	// Nothing is credited while the section reads empty or negative, so belt
	// noise around zero cannot accumulate into a total over a long day. The
	// app's own empty-belt threshold sits above this and decides what counts
	// as crop; this only stops the arithmetic running backwards.
	if (delta > 0 && ScaleLb > 0 && ScaleSectionLenX10 > 0)
	{
		double travelIn = (double)delta * (double)ScaleInPerPulseX1000 / 1000.0;
		double sectionIn = (double)ScaleSectionLenX10 / 10.0;
		CumPounds += ScaleLb * travelIn / sectionIn;
	}
}

// ── State for the packet ──────────────────────────────────────────────────

bool ScaleIsOK()
{
	return ScaleFound
		&& !ScaleOverload
		&& (millis() - ScaleLastReadMs) < ScaleStaleMs;
}

// The belt counts as running while pulses keep arriving. The timeout is the
// app's, so a slow belt on a coarse roller is not called stopped.
bool BeltIsRunning()
{
	noInterrupts();
	uint32_t lastUs = LastBeltEdgeUs;
	interrupts();
	if (lastUs == 0) return false;

	uint32_t stopUs = (uint32_t)ScaleBeltStopX10 * 100000UL;   // tenths of a second → µs
	return (micros() - lastUs) < stopUs;
}

// The app is told the module holds a zero once it has sent one. Before that the
// readings are raw and the app shows "Zero" on the status bar.
bool ScaleIsTared()
{
	return ScaleSettingsSeen;
}

bool SettingsAreFresh()
{
	return ScaleSettingsSeen && (millis() - ScaleSettingsMs) < SettingsFreshMs;
}

// Cumulative pounds as the tenths the packet carries, wrapped into a uint32 the
// same way the pulse counter wraps.
uint32_t CumPoundsX10()
{
	double tenths = CumPounds * 10.0;
	if (tenths < 0) return 0;
	return (uint32_t)fmod(tenths, 4294967296.0);
}

// Live section load as tenths of a pound, clamped to the int16 the packet has
// room for. ±3276.7 lb is far beyond any weighed section.
int16_t ScaleLbX10()
{
	double v = ScaleLb * 10.0;
	if (v >  32767) return  32767;
	if (v < -32768) return -32768;
	return (int16_t)v;
}

// ── Settings from the PC ──────────────────────────────────────────────────
// The 13-byte block is the app's ModuleSettings.Block, little-endian:
//   [0-3]   zero_counts        int32
//   [4-7]   span_lb_per_count  float32
//   [8-9]   section_len_in×10  uint16
//   [10-11] in_per_pulse×1000  uint16
//   [12]    belt_stop_s×10     uint8
//
// Applied only when the CRC-16 matches, so a damaged message leaves the module
// weighing with what it already had rather than with rubbish.
void ApplyScaleSettings(const byte block[13])
{
	int32_t zero;
	float   span;
	memcpy(&zero, block, 4);
	memcpy(&span, block + 4, 4);

	uint16_t sectionX10 = (uint16_t)block[8]  | ((uint16_t)block[9] << 8);
	uint16_t perPulse   = (uint16_t)block[10] | ((uint16_t)block[11] << 8);

	// A zero section length would divide by zero in the integration, and a span
	// of zero would record nothing at all; both mean a bad message got through.
	if (sectionX10 == 0 || span == 0.0f) return;

	ScaleZeroCounts       = zero;
	ScaleSpanLbPerCnt     = span;
	ScaleSectionLenX10    = sectionX10;
	ScaleInPerPulseX1000  = perPulse;
	ScaleBeltStopX10      = block[12];

	ScaleSettingsSeen = true;
	ScaleSettingsMs   = millis();
}

// CRC-16/CCITT-FALSE: polynomial 0x1021, initial 0xFFFF, no reflection, no
// final XOR. Must match ModuleSettings.Crc16 in the app exactly.
uint16_t Crc16CCITT(const byte data[], byte length)
{
	uint16_t crc = 0xFFFF;
	for (byte i = 0; i < length; i++)
	{
		crc ^= (uint16_t)data[i] << 8;
		for (byte b = 0; b < 8; b++)
			crc = (crc & 0x8000) ? (uint16_t)((crc << 1) ^ 0x1021) : (uint16_t)(crc << 1);
	}
	return crc;
}
