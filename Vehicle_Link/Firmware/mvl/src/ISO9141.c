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

#define BAUD_10400_BSCALE	-4
#define BAUD_10400_BSEL		3061

#define UART_ISO9141	USARTD0

struct byte_buffer iso9141_rx_buffer;
struct byte_buffer iso9141_tx_buffer;
bool fiveBaudInitActive = false;
uint8_t fiveBaudByte = 0x00;


// this value increments forward for every bit sent/received. The breakdown for how it changes is as follows:
//  0 = initial, disable TX and RX to prevent XMEGA UART from interfering and pull ISO9141_TX low to send start bit
//  1-8 = transmit bit X of the requested five-baud-init byte, usually 0x33
//  9 = send stop bit
//  10 = set TX high to cease transmission
//  11 = Wait for init time-out. If time-out is reached, TX and RX is turned back on, and we cease five baud processing
//  12 = received start bit from ECU
//  13-20 = receive keyword 1 bits
//  21 = receive stop bit
//  22 = Wait for init time-out, same as 11
//  12 = received start bit from ECU
//  13-20 = receive keyword 2 bits
//  21 = receive stop bit, re-enable RX and TX, resume 10.4 kbps communication, send keyword bytes to USB
uint8_t fiveBaudStep = 0xFF;
uint8_t keyword1 = 0x00, keyword2 = 0x00;

#define ISO9141_TIMER TCD1

void iso9141_setup()
{
	iso9141_rx_buffer.idxCurr = iso9141_rx_buffer.idxLast = 0;
	iso9141_tx_buffer.idxCurr = iso9141_tx_buffer.idxLast = 0;
	PORTD.DIRSET |= PIN_ISO9141_TX;
	usart_setup(&UART_ISO9141, BAUD_10400_BSCALE, BAUD_10400_BSEL);
}

void iso9141_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer)
{
	if(fiveBaudInitActive)
	{
		// send start/stop bit
		if(fiveBaudStep == 0x00)
		{
			usart_tx_disable(&UART_ISO9141);
			usart_rx_disable(&UART_ISO9141);
			PORTD.OUTCLR |= PIN_ISO9141_TX;
			ISO9141_TIMER.CNT = 0;
			fiveBaudStep++;
		}
		else if(fiveBaudStep <= 9)
		{
			if(ISO9141_TIMER.CNT >= 200)
			{
				if(fiveBaudByte & 1 || fiveBaudStep == 9) PORTD.OUTCLR |= PIN_ISO9141_TX;
				else PORTD.OUTSET |= PIN_ISO9141_TX;
				fiveBaudByte >>= 1;	
				ISO9141_TIMER.CNT = 0;
				fiveBaudStep++;
			}
		}
		else if(fiveBaudStep == 10)
		{
			if(ISO9141_TIMER.CNT >= 200)
			{
				PORTD.OUTSET |= PIN_ISO9141_TX;
				ISO9141_TIMER.CNT = 0;
				fiveBaudStep++;
			}
		}
	}
	if(UART_ISO9141.STATUS & USART_RXCIF_bm)
	{
		//uint8_t rxByte = UART_ISO9141.DATA;
	}
}

void iso9141_start_five_baud_init(uint8_t initByte)
{
	fiveBaudByte = initByte;
	fiveBaudInitActive = true;
	fiveBaudStep = 0xFF;
	keyword1 = keyword2 = 0x00;
}