# CDJPScanMaster

Tool(s) based on work done by MWisBest on his DRBDBReader, and laszlodaniel and Chris O on the Chrysler CCD/SCI Scanner.

The DRBDBReader I've built works similarly to how MWisBest's works, except the text entry box does nothing. Once it loads the database, it simply shows the module selections on the left, and all the TX's on the right. TX's are the data entries that drive the whole tool. When you open a data list (like Actuators, Input/Output, etc) and click a data item (Engine RPM, etc) the associated TX is shown on the right. I'm aware the actual data shown is minimal currently, I'm mostly using this tool to build the associations of body style (TJ, XJ, ZJ, etc) to the different modules, so the actual scanner can auto-detect the module.

Currently this repo contains a rewritten version of DRBDBReader, with several of the gaps filled in based on my own research. The updated tool is able to view the hierarchy the way that DRB can, from module selection (Engine/Transmission/etc) all the way down to a given ECU's data lists. Currently does not connect to anything, my Arduino is only wired for CCD, so once I can get it wired up for CCD, SCI, and J1850, I am going to start testing. I'd very much appreciate the input of someone more skilled than me in drawing up schematics for a PCB that I can use for all 3 protocols, and also ISO9141 in the future.
