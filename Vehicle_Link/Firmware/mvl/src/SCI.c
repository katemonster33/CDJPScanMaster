/*
 * SCI.c
 *
 * Created: 3/5/2018 4:01:57 PM
 *  Author: rk5642
 */ 
#include "main.h"
#include <string.h>

//void sci_idle_timer_stop(void);
//void sci_idle_timer_start(void);
//void sci_idle_timer_setup(void);
//static void sci_bus_idle_callback(void);

#define UART_SCI		USARTE0

uint8_t recv_buff[255]; /* CCD receive buffer / MAX 8 Bytes */
int recv_buff_len = 0; /* CCD receive buffer pointer */

uint8_t send_buff[255];
int send_buff_len = 0;
int send_cur_index = -1;
bool inHighSpeedMode = false;
uint16_t currentBitTime = 0; // time (in uS) length of a single UART bit.
bool busIdle = false;

void sci_setup(void)
{
	PORTE.PIN2CTRL = PORT_OPC_PULLUP_gc;
	usart_setup(&UART_SCI, 7812);
	//sci_idle_timer_setup();
	currentBitTime = 128 / 2; // 128 uS * (1 uS / 2 ticks) = 64 ticks
    
}

void sci_lo_speed(void)
{
	usart_set_baudrate(&UART_SCI, 7812, sysclk_get_per_hz());
	currentBitTime = 128 / 2; // 128 uS * (1 uS / 2 ticks) = 64 ticks
}

void sci_hi_speed(void)
{
	usart_set_baudrate(&UART_SCI, 62500, sysclk_get_per_hz());
	currentBitTime = 8 / 2; // 8 uS * (1 uS / 2 ticks) = 4 ticks
}

void sci_do_tasks(struct byte_buffer *txBuffer)
{
	if(txBuffer->idxLast > 0)
	{
		memcpy(send_buff, txBuffer->bytes, txBuffer->idxLast);
		send_buff_len = txBuffer->idxLast;
		send_cur_index = 0;
		txBuffer->idxLast = 0;
	}
	
	if(send_cur_index == 0)
	{
		UART_SCI.DATA = send_buff[send_cur_index];
		send_cur_index++;
	}
}

// Interrupt for when data is received on the SCI UART
ISR(USARTE0_RXC_vect)
{
	recv_buff[recv_buff_len] = UART_SCI.DATA;
	recv_buff_len++;
	if(send_cur_index > 0 && send_cur_index < send_buff_len)
	{
		UART_SCI.DATA = send_buff[send_cur_index];
		send_cur_index++;
		if(send_cur_index == send_buff_len)
		{
			send_cur_index = -1;
			send_buff_len = 0;
		}
	}
	else
	{
		usb_queue_rx(recv_buff, recv_buff_len, PAYLOAD_PROTOCOL_SCI);
		recv_buff_len = 0;
	}
}