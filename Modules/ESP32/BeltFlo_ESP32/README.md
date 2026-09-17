# BeltFlo_ESP32

BeltFlo conveyor-scale module firmware for the ESP32 (DOIT ESP32 DEVKIT V1).
Weighs the crop crossing a harvester's weighed belt section with an NAU7802
24-bit bridge converter, counts belt travel from a proximity sensor, and sends
cumulative pounds and pulses to the PC app over **WiFi UDP, wired Ethernet UDP
(W5500) or CAN bus** — selectable at runtime in the web portal.

The module does the integrating rather than the app:

    pounds = load on the weighed section × (belt travel ÷ section length)

The app pushes the zero, span, section length and belt travel per pulse down to
the module and reads back a cumulative counter it differences. That is why a
dropped packet costs nothing — the next one carries the running total — and why
the module has to hold the same calibration the app does.

Settings are stored in EEPROM and edited through the module's web portal;
nothing needs to be recompiled to reconfigure.

## Web portal

The module always runs a WiFi access point, even in CAN mode:

- AP name: `BeltFlo_ESP32_<8 hex digits of the MAC>`; open network unless an
  AP password of 8+ characters is set in the portal.
- Portal address: `192.168.<200 + module ID>.1` (default ID 0 → `192.168.200.1`).
  A captive-portal DNS redirects any address, and mDNS answers at `beltflo`.
- Firmware update page: `/update` (ESP2SOTA). Prebuilt images live one folder
  up (`BeltFlo_ESP32.ino.bin`).

Portal settings:

| Setting | Meaning |
|---|---|
| Comm mode | WiFi UDP, CAN bus, or Ethernet UDP (W5500) |
| Ethernet subnet | First three octets of the wired network (default `192.168.1`). Module IP is `subnet.(50 + ID)`, /24, broadcast to `subnet.255`. In Ethernet mode the portal shows W5500/link status |
| Network / Password + Connect | Optional station mode: also join an existing WiFi network (default `Tractor`). After repeated failures the module reverts to AP-only and restarts |
| AP password | Password for the module's own access point |

The portal also shows the scale read-only: whether the converter answered, raw
counts, the live section load, the belt pulse count and whether settings are
still arriving from the app. None of it is editable here — the calibration is
the app's, on its Conveyor Setup and Scale Calibration screens.

Settings are validated against an EEPROM layout version (`StructVersion`) —
bumping it in the source wipes stored settings back to defaults.

## Hardware / pins (defaults)

| Signal | GPIO | Notes |
|---|---|---|
| Belt travel sensor | 34 | One pulse per target on a belt roller, rising edge. Input-only pin with no internal pull-up — an open-collector sensor needs an external one |
| NAU7802 SDA | 21 | Shared I2C bus |
| NAU7802 SCL | 22 | Same |
| CAN TX | 14 | → MCP2562 TXD |
| CAN RX | 27 | ← MCP2562 RXD |
| W5500 SS | 5 | Ethernet board chip select (same wiring as AOG_RC) |
| W5500 SCK / MISO / MOSI | 18 / 19 / 23 | VSPI defaults; W5500 also needs 3.3 V + GND |
| Debug UART | USB serial | 38400 baud, boot messages + CAN warnings |

Pin assignments live in `ModuleConfig` (EEPROM) but are not exposed on the
portal — change the defaults in the source if a board revision moves them.

The converter is set up for a bridge load cell: internal LDO at 3.3 V, gain
×128, 80 samples per second, with an internal offset calibration at boot that
is checked for both completion and `CAL_ERR`. That zeroes the converter's own
offset, not the belt's — the belt zero is the app's, measured with the belt
running empty and sent down as `zero_counts`.

## Building

Arduino IDE (or arduino-cli) with the **esp32 core** (tested with 3.3.7),
board **DOIT ESP32 DEVKIT V1**. One external library: **Ethernet_Generic**
(install via Library Manager — the same library AOG_RC uses for the W5500).
The modified ESP2SOTA OTA library is bundled in `src/ESP2SOTA_RC/` (it must
stay under `src/` so Arduino builds compile it). The `.vcxproj` / `__vm`
files are a Visual Micro project for building from Visual Studio; plain
Arduino IDE users can ignore them.

Note that Arduino concatenates the `.ino` files alphabetically, so `Comm.ino`
is compiled before `Scale.ino`. Shared state therefore lives in the main
sketch: functions are auto-prototyped across files, variables are not.

## Protocol

Ports are BeltFlo's own — **30300** module → PC, **30400** PC → module — so a
grain YieldFlo module and a conveyor BeltFlo module can share one network
without reading each other's packets.

### Module → PC

Conveyor packet, 5 Hz. Flags in both transports:

| Bit | Meaning |
|---|---|
| 0 | ScaleOK — the converter is answering and inside its range |
| 1 | BeltRunning — pulses still arriving |
| 2 | Tared — the module holds a zero from the app |
| 3 | ReceivingFromPC — a valid settings message within the last 4 s |
| 4 | Overload — cells at their rated limit |

**WiFi / Ethernet UDP** — 19 bytes, PGN 40010 LE:

| Bytes | Field |
|---|---|
| 0-1 | PGN 40010 LE (`0x4A 0x9C`) |
| 2 | flags |
| 3-6 | cum_pounds_x10 uint32 LE, wraps |
| 7-10 | cum_pulses uint32 LE, wraps |
| 11-12 | scale_lb_x10 int16 LE, live load on the section after zero |
| 13-16 | scale_raw int32 LE, raw converter counts |
| 17 | reserved |
| 18 | CRC8, byte sum of everything before it |

**CAN bus** — 250 kbps, extended IDs, DLC 8. Eight bytes will not hold both the
counters and the live weight, so it goes as two frames. The status frame is
transmitted first, so a counter row is never logged against the previous
cycle's flags:

- Status, `0x18FF03F8`: [0]=flags, [1-2]=scale_lb_x10, [3-6]=scale_raw int32, [7]=reserved
- Counters, `0x18FF02F8`: [0-3]=cum_pounds_x10, [4-7]=cum_pulses

### PC → module

The settings block, 13 bytes little-endian, is what lets the module weigh:

| Bytes | Field |
|---|---|
| 0-3 | zero_counts int32 — raw counts with the belt running empty |
| 4-7 | span_lb_per_count float32 |
| 8-9 | section_len_in_x10 uint16 |
| 10-11 | in_per_pulse_x1000 uint16 |
| 12 | belt_stop_s_x10 uint8 |

Sent every 2 s as a heartbeat and at once when it changes. The repeat is what
sets flags bit 3, so the app can tell a module that is merely transmitting from
one that is also listening.

**UDP** — 18 bytes, PGN 40011 LE (`0x4B 0x9C`), to module port 30400: PGN,
the 13-byte block, CRC-16 LE of the block, then CRC8 over everything before it.

**CAN** — two frames from source address `0xF9` (the PC):

- `0x18FF04F9`: block bytes 0-7
- `0x18FF05F9`: block bytes 8-12, then the CRC-16 LE, then a spare byte

Applied only when the CRC-16 matches over the reassembled block, and only when
the section length and span are non-zero — a bad message leaves the module
weighing with what it already had.

CRC-16 is CCITT-FALSE: polynomial `0x1021`, initial `0xFFFF`, no reflection, no
final XOR. It must match `ModuleSettings.Crc16` in the app exactly.

CAN Bus Off is retried every 3 s; after 5 failed recoveries the module falls
back to WiFi for the session (EEPROM unchanged — CAN returns on restart).

## Firmware version

`InoID` encodes the build date as DDMMY (e.g. 4076 → 2026-07-04). Update it
with every build; the boot banner prints it decoded.

## Status

Converted from the YieldFlo grain firmware and **not yet run on hardware** —
compile-verified only. The NAU7802 setup in particular wants a bench check
against the datasheet before anyone trusts a weight from it.
