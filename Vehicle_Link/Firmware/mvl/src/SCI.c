/*
 * SCI.c
 *
 * Created: 3/5/2018 4:01:57 PM
 *  Author: rk5642
 */ 
#include "main.h"
#include <string.h>

#define UART_SCI		USARTE0

uint8_t recv_buff[255]; /* CCD receive buffer / MAX 8 Bytes */
int recv_buff_len = 0; /* CCD receive buffer pointer */

uint8_t send_buff[255];
int send_buff_len = 0;
int send_cur_index = -1;
bool inHighSpeedMode = false;
uint16_t currentBitTime = 128; // time (in uS) length of a single UART bit.

void sci_setup(void)
{
	PORTE.PIN2CTRL = PORT_OPC_PULLUP_gc;
	usart_setup(&UART_SCI, 7812);
        TCD1.CNT = 0;// Zeroise count
	TCD1.PER = 200; //Period
	TCD1.CTRLA = TC_CLKSEL_DIV8_gc; //Divider 
	TCD1.INTCTRLA = TC_OVFINTLVL_LO_gc; //Low level interrupt
	TCD1.INTFLAGS = 0x01; // clear any initial interrupt flags 
	TCD1.CTRLB = TC_WGMODE_NORMAL_gc; // Normal operation
}

void sci_lo_speed(void)
{
	usart_set_baudrate(&UART_SCI, 7812, sysclk_get_per_hz());
	currentBitTime = 128;
}

void sci_hi_speed(void)
{
	usart_set_baudrate(&UART_SCI, 62500, sysclk_get_per_hz());
	currentBitTime = 8;
}

void sci_do_tasks(struct byte_buffer *readBuffer, struct byte_buffer *txBuffer)
{
	if(txBuffer->idxLast > 0)
	{
		memcpy(send_buff, txBuffer->bytes, txBuffer->idxLast);
		send_buff_len = txBuffer->idxLast;
		send_cur_index = 0;
		txBuffer->idxLast = 0;
	}
	if(UART_SCI.STATUS & USART_RXCIF_bm)
	{
		recv_buff[recv_buff_len] = UART_SCI.DATA;
		recv_buff_len++;
		if(send_cur_index > 0)
		{
			if(!inHighSpeedMode && send_cur_index == 1 && send_buff[0] == 0x12 && recv_buff[0] == 0x12)
			{
				sci_hi_speed();
				inHighSpeedMode = true;
			}
			if(send_cur_index < send_buff_len)
			{
				usart_putchar(&UART_SCI, send_buff[send_cur_index]);
				send_cur_index++;
				if(send_cur_index == send_buff_len)
				{
					send_cur_index = -1;
					send_buff_len = 0;
				}
			}
		}
		TCD1.CNT = 0;
	}
	if(recv_buff_len > 0 && TCD1.CNT > currentBitTime * 4 * 11)
	{
		memcpy(readBuffer->bytes, recv_buff, recv_buff_len);
		readBuffer->idxCurr = 0;
		readBuffer->idxLast = recv_buff_len;
		recv_buff_len = 0;
	}
	if(send_cur_index == 0)
	{
		usart_putchar(&UART_SCI, send_buff[send_cur_index]);
		send_cur_index++;
	}
	
	if(inHighSpeedMode && send_cur_index > 0 && send_buff[send_cur_index - 1] == 0xFE)
	{
		sci_lo_speed();
		inHighSpeedMode = false;
	}
}