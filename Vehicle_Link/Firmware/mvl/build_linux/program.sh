#!/bin/bash
make clean;
make all && avrdude -v -p x32a4u -c atmelice_pdi -B 4Mhz -Uflash:w:mvl.hex:i
