/*
 * definitions.h
 *
 * Created: 2/5/2018 3:53:02 PM
 *  Author: Katie M
 */ 


#ifndef DEFINITIONS_H_
#define DEFINITIONS_H_
#include <stdint.h>

#define PIN_SCI_A_ENGINE_RX_EN	PIN0_bm // PORT B, active high
#define PIN_SCI_B_ENGINE_RX_EN	PIN1_bm // PORT B, active high
#define PIN_SCI_A_TRANS_RX_EN	PIN2_bm // PORT B, active high
#define PIN_SCI_B_TRANS_RX_EN	PIN3_bm // PORT B, active high

#define PIN_CCD_RX				PIN2_bm // PORT C
#define PIN_CCD_TX				PIN3_bm // PORT C

#define PIN_ISO_K_EN			PIN5_bm // PORT D, active low

#define PIN_SCI_A_TX_EN			PIN0_bm // PORT E, active low
#define PIN_SCI_RX				PIN2_bm // PORT E
#define PIN_SCI_TX				PIN3_bm // PORT E

#define BAUD_7812_BSCALE	0
#define BAUD_7812_BSEL		383
#define BAUD_62500_BSCALE	0
#define BAUD_62500_BSEL		31

#define BYTE_BUFFER_SIZE	64
struct byte_buffer
{
	uint8_t bytes[BYTE_BUFFER_SIZE];
	uint8_t idxCurr;
	uint8_t idxLast;
};

#endif /* DEFINITIONS_H_ */