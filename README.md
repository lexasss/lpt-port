# LPT Port Tool

Actions available:

1. Reads from LPT all ports every 10 ms, prints the data if it is different from the previous read. *
2. Reads and prints out all pins as Int32
3. Writes to data port numbers from 0 to 255 in sequence
4. Sets all data pint to HIGH
5. Clears all data pint to LOW
6. Sets or clears a single data pin

* all actions are applied to the whole range of ports (e.g., to 8 ports if LPT spans over the range 0x378 - 0x37F)

**Note**: the tool was tested on Win10 PC where LPT spans over 0x378 - 0x37F.