#include <AltSoftSerial.h>
#include <SoftwareSerial.h>
/*
  Code adapted from Chris O. and Juha Niinikoski

  Prints ID Byte, sent via serial monitor from PC, to serial monitor.
*/
#define PIN_SCI_RX 4
#define PIN_SCI_A_ENGINE_TX 5
#define PIN_SCI_B_ENGINE_TX 6

#define CH_NONE             0
#define CH_CCD              1
#define CH_SCI_A_ENGINE     2
#define CH_SCI_B_ENGINE     3
#define CH_J1850            4
#define CH_ISO9141          5
#define CH_SCI_A_TRANS      6
#define CH_SCI_B_TRANS      7


int activeChannel = CH_NONE;
int idlePin = 3;      //Idle pin /INT0 on Atmega328P
//const int PWMPin = 9; //PWM~ Pin#

byte recv_buff[255]; /* CCD receive buffer / MAX 8 Bytes */
int recv_buff_len = 0; /* CCD receive buffer pointer */

byte send_buff[255];
int send_buff_len = 0;
int send_cur_index = -1;

char serialBuff[100];
int serialBuffLen = 0;

AltSoftSerial ccd; // RX, TX
SoftwareSerial sci_a_ecm(PIN_SCI_RX, PIN_SCI_A_ENGINE_TX);
SoftwareSerial sci_b_ecm(PIN_SCI_RX, PIN_SCI_B_ENGINE_TX);


void setup() 
{
  pinMode(idlePin, INPUT);            //set idle pin for input
  Serial.begin(115200);
  Serial.println("OKGO");
}

void loop() 
{
  if(activeChannel == CH_CCD)
  {
    ccd_send_receive();
  }
  else if(activeChannel == CH_SCI_A_ENGINE)
  {
    sci_send_receive(&sci_a_ecm);
  }
  else if(activeChannel == CH_SCI_B_ENGINE)
  {
    sci_send_receive(&sci_b_ecm);
  }
  if(Serial.available())
  {
    char sendMsgChar = Serial.read();
    if(sendMsgChar == '\n')
    {
      if(serialBuffLen == 1) //channel set command
      {
        disconnectChannels();
        activeChannel = fromHex(serialBuff[0]);
        
        if(activeChannel == CH_CCD)
        {
          ccd.begin(7812.5);
        }
        else if(activeChannel == CH_SCI_A_ENGINE)
        {
          pinMode(PIN_SCI_A_ENGINE_TX, OUTPUT);
          digitalWrite(PIN_SCI_A_ENGINE_TX, HIGH);
          sci_a_ecm.begin(7812.5);
        }
        else if(activeChannel == CH_SCI_B_ENGINE)
        {
          pinMode(PIN_SCI_B_ENGINE_TX, OUTPUT);
          digitalWrite(PIN_SCI_B_ENGINE_TX, HIGH);
          sci_b_ecm.begin(7812.5);
        }
        Serial.print("CHANNEL SET ");
        Serial.println(activeChannel, HEX);
        serialBuffLen = 0;
      }
      else
      {
        send_buff_len = 0;
        for(int i = 0; i < serialBuffLen; i += 2)
        {
          send_buff[send_buff_len] = fromHex(serialBuff[i], serialBuff[i + 1]);
          send_buff_len++;
        }
        if(activeChannel == CH_CCD)
        {
          send_buff[send_buff_len] = ccd_make_crc8(send_buff, send_buff_len);
          send_buff_len++;
        }
        serialBuffLen = 0;
        send_cur_index = 0; //start trying to send
      }
    }
    else
    {
      serialBuff[serialBuffLen++] = sendMsgChar;
    }
  }
}

void disconnectChannels()
{
  pinMode(PIN_SCI_A_ENGINE_TX, INPUT);
  pinMode(PIN_SCI_B_ENGINE_TX, INPUT);
  sci_a_ecm.end();
  sci_b_ecm.end();
  ccd.end();
  send_buff_len = 0;
  send_cur_index = -1;
}

bool inHighSpeedMode = false;

inline void sci_send_receive(SoftwareSerial *sci_channel)
{
  sci_channel->listen();
  if(sci_channel->available())
  {
    recv_buff[recv_buff_len] = sci_channel->read();
    recv_buff_len++;
    if(send_cur_index > 0)
    {
      if(!inHighSpeedMode && send_cur_index == 1 && send_buff[0] == 0x12 && recv_buff[0] == 0x12)
      {
        sci_channel->begin(62500);
        inHighSpeedMode = true;
      }
      if(send_cur_index < send_buff_len)
      {
        sci_channel->write(send_buff[send_cur_index]);
        send_cur_index++;
      }
      else
      {
        send_cur_index = -1;
        send_buff_len = 0;
        printRecvBuff();
      }
    }
    else
    {
      send_cur_index = -1;
      send_buff_len = 0;
      printRecvBuff();
    }
  }
  if(send_cur_index == 0)
  {
    sci_channel->write(send_buff[0]);
    send_cur_index++;
  }
  
  if(inHighSpeedMode && send_cur_index > 0 && send_buff[send_cur_index - 1] == 0xFE)
  {
    sci_channel->begin(7812.5);
    inHighSpeedMode = false;
  }
}

int lastIdlePinValue = HIGH;

inline void ccd_send_receive()
{
  if (ccd.available()) 
  {
    byte data = ccd.read();
    recv_buff[recv_buff_len] = data;    // read & store character
    recv_buff_len++;                   // increment the pointer to the next byte
    if(send_cur_index > 0 && send_cur_index < send_buff_len)
    {
      if(send_buff[send_cur_index - 1] != recv_buff[recv_buff_len - 1])
      {
        send_cur_index = 0;
      }
      else
      {
        ccd.write(send_buff[send_cur_index]);
        send_cur_index++;
        if(send_cur_index == send_buff_len) //sending done
        {
          send_buff_len = 0;
          send_cur_index = -1; //no message to send
        }
      }
    }
  }
  int newIdlePinValue = digitalRead(idlePin);
  if (lastIdlePinValue == HIGH && newIdlePinValue == LOW) // check the CDP68HC68S1 IDLE pin interrupt flag, change from Low to High.
  { 
    ccd_process_data(); // GOTO process_data loop
    if(send_buff_len != 0 && send_cur_index == 0 /*&& newIdlePinValue == LOW*/)
    {
      ccd.write(send_buff[send_cur_index]);
      send_cur_index++;
    }
  }
  lastIdlePinValue = newIdlePinValue;
}

inline byte fromHex(char firstNibble, char secondNibble)
{
  return (fromHex(firstNibble) << 4) | fromHex(secondNibble);
}

byte fromHex(char nibble)
{
  if(nibble >= '0' && nibble <= '9')
  {
    return nibble - '0';
  }
  else if(nibble >= 'a' && nibble <= 'f')
  {
    return (nibble - 'a') + 10;
  }
  else if(nibble >= 'A' && nibble <= 'F')
  {
    return (nibble - 'A') + 10;
  }
  return 0;
}

byte ccd_make_crc8(byte *msgPtr, byte msgLen)
{
  byte crc = 0;
  for(int i = 0; i < msgLen; i++)
  {
    crc += msgPtr[i];
  }
  return crc;
}

void ccd_process_data() 
{
  byte expectedCrc = ccd_make_crc8(recv_buff, recv_buff_len - 1);
  if(expectedCrc != recv_buff[recv_buff_len - 1])
  {
    recv_buff_len = 0; // RESET buffer pointer to byte 0 for data to be overwritten
    return;
  }
  recv_buff_len--;
  printRecvBuff();
}

inline void printRecvBuff()
{
  for(int i = 0; i < recv_buff_len; i++)
  {
    if(recv_buff[i] < 0x10) Serial.print("0");
    Serial.print(recv_buff[i], HEX);
  }
  Serial.println();
  recv_buff_len = 0; // RESET buffer pointer to byte 0 for data to be overwritten
}

