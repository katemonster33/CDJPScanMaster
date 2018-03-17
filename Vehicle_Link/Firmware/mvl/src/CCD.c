/*
 * CCD.c
 *
 * Created: 3/2/2018 12:04:17 AM
 *  Author: rk5642
 */
 #include "main.h"
 #include "usart.h"

#define UART_CCD		USARTC0

void ccd_setup()
{
	PORTC.DIRSET = PIN_CCD_TX;
	//PORTC.OUTSET = PIN_CCD_TX; // write it high immediately so we aren't driving the bus accidentally
	usart_setup(&UART_CCD, 7812);
}

void ccd_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer)
{
	if(UART_CCD.STATUS & USART_RXCIF_bm)
	{
		readBuffer->bytes[0] = UART_CCD.DATA;
		readBuffer->idxLast = 1;
		UART_CCD.STATUS &= ~USART_RXCIF_bm;
	}
}