# LPT Port Sniffer

Actions available:

1. Read data from LPT ports every 10 ms, prints the data if it is different from the previous read.
2. Write in sequence numbers from 0 to 255
3. Write in sequence numbers from 0 to 255, read the ports back, and print those that differ from what was set

**Note**: all actions are applied to the whole range of ports (e.g., to 8 ports if LPT spans over the range 0x378 - 0x37F)

**Note**: the tool was tested on Win10 PC where LPT spans over 0x378 - 0x37F. The pairs of ports 0x378/0x37C, 0x379/0x37D, 0x37A/0x37E and 0x37B/0x37F are identical, and setting a value to then ports result in the following outcome:
* ports 0x378/0x37C (data): any value can be set
* ports 0x379/0x37D (status): always equal to 0x7F
* ports 0x37A/0x37E (control): the upper 2 bits are always ON, i.e. the set value is OR'ed with 0xC0
* ports 0x37B/0x37F (unused?): always equal to 0xFF
