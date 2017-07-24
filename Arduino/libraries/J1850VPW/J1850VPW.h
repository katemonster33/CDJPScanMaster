#include "Arduino.h"

uint8_t crc8(uint8_t *buffer, uint8_t len);
void handleIncomingPulse(int busState, int pulseWidth);
inline void pushReceivedBit(int rxBit);

class J1850VPW
{
public:
	J1850VPW() { }
	void setup(int pin);
	bool readBytes(uint8_t *buffer, uint8_t &len);
	bool sendBytes(uint8_t *buffer, uint8_t len);
};