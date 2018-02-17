/*
 * ISO9141_CCD_SCI.c
 *
 * Created: 2/5/2018 6:06:42 PM
 *  Author: Katie M
 */ 
#include "main.h"
#include "usart.h"

#define PIN_ISO9141_RX			PIN2_bm // PORT D
#define PIN_ISO9141_TX			PIN3_bm // PORT D

#define UART_ISO9141	USARTD0

struct byte_buffer rxBuffer;
struct byte_buffer txBuffer;

void iso9141_setup()
{
	rxBuffer.idxCurr = rxBuffer.idxLast = 0;
	txBuffer.idxCurr = txBuffer.idxLast = 0;
	usart_setup(&UART_ISO9141, BAUD_10400_BSCALE, BAUD_10400_BSEL);
}

void iso9141_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer)
{
	if(UART_ISO9141.STATUS & USART_RXCIF_bm)
	{
		//uint8_t rxByte = UART_ISO9141.DATA;
	}
}