/*
 * J1850VPW.h
 *
 * Created: 1/23/2018 1:06:31 PM
 *  Author: rk5642
 */ 


#ifndef J1850VPW_H_
#define J1850VPW_H_

// receiving pulse width
#define MICROSEC_TO_CNT_SCALE 1

uint16_t RX_SHORT_MIN	        = 16 * MICROSEC_TO_CNT_SCALE;	// minimum short pulse time
uint16_t RX_SHORT_MAX         = 96 * MICROSEC_TO_CNT_SCALE;	// maximum short pulse time
uint16_t RX_LONG_MIN          = 96 * MICROSEC_TO_CNT_SCALE;	// minimum long pulse time
uint16_t RX_LONG_MAX		      = 163 * MICROSEC_TO_CNT_SCALE;	// maximum long pulse time
uint16_t RX_SOF_MIN		        = 163 * MICROSEC_TO_CNT_SCALE;	// minimum start of frame time
uint16_t RX_SOF_MAX		        = 239 * MICROSEC_TO_CNT_SCALE;	// maximum start of frame time
uint16_t RX_EOD_MIN		        = 163 * MICROSEC_TO_CNT_SCALE;	// minimum end of data time
uint16_t RX_EOD_MAX		        = 239 * MICROSEC_TO_CNT_SCALE;	// maximum end of data time
uint16_t RX_EOF_MIN		        = 239 * MICROSEC_TO_CNT_SCALE;	// minimum end of frame time, ends at minimum IFS
uint16_t RX_BRK_MIN		        = 239 * MICROSEC_TO_CNT_SCALE;	// minimum break time
uint16_t RX_IFS_MIN		        = 280 * MICROSEC_TO_CNT_SCALE;	// minimum inter frame separation time, ends at next SOF

uint8_t TX_SHORT             = 64 / MICROSEC_TO_CNT_SCALE;   // Short pulse nominal time
uint8_t TX_LONG              = 128 / MICROSEC_TO_CNT_SCALE;    // Long pulse nominal time
uint8_t TX_SOF               = 200 / MICROSEC_TO_CNT_SCALE;    // Start Of Frame nominal time
uint8_t TX_EOD               = 200 / MICROSEC_TO_CNT_SCALE;    // End Of Data nominal time
uint8_t TX_EOF               = 280 / MICROSEC_TO_CNT_SCALE;    // End Of Frame nominal time
uint8_t TX_BRK               = 300 / MICROSEC_TO_CNT_SCALE;    // Break nominal time
uint8_t TX_IFS               = 280/ MICROSEC_TO_CNT_SCALE;    // Inter Frame Separation nominal time

void j1850vpw_enable();
short j1850vpw_check_read(char *readBuffer);
short j1850vpw_check_write(char *readBuffer);

#endif /* J1850VPW_H_ */