/*
 * definitions.h
 *
 * Created: 2/5/2018 3:53:02 PM
 *  Author: Katie M
 */ 


#ifndef DEFINITIONS_H_
#define DEFINITIONS_H_
#include <stdint.h>

#define PAYLOAD_PROTOCOL_J1850		1
#define PAYLOAD_PROTOCOL_CCD		53
#define PAYLOAD_PROTOCOL_SCI		60
#define PAYLOAD_PROTOCOL_ISO9141	155

#define RC_SUCCESS				64
#define RC_FAIL					65

#define PIN_SCI_A_TX_EN			PIN0_bm // PORT B, active HIGH
#define PIN_SCI_B_TX_EN			PIN1_bm // PORT B, active HIGH

#define PIN_ISO_K_EN			PIN5_bm // PORT D, active HIGH

#define PIN_SCI_A_RX_EN			PIN4_bm // PORT C, active HIGH
#define PIN_SCI_B_RX_EN			PIN5_bm // PORT C, active HIGH
#define PIN_SCI_RX				PIN2_bm // PORT E
#define PIN_SCI_TX				PIN3_bm // PORT E

#define BAUD_7812_BSCALE	0
#define BAUD_7812_BSEL		383
#define BAUD_62500_BSCALE	0
#define BAUD_62500_BSEL		31

#define BYTE_BUFFER_SIZE	127
struct byte_buffer
{
	uint8_t bytes[BYTE_BUFFER_SIZE];
	uint8_t idxCurr;
	uint8_t idxLast;
};

#endif /* DEFINITIONS_H_ */