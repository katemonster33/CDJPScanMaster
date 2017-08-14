using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanMaster.Comms
{
    internal enum ModuleTypeID
    {
        Engine,
        Transmission,
        Body,
        Brakes,
        VehicleTheftSecurity,
        AirTempControl = 6,
        InstrumentCluster,
        AirBag,
        Otis,
        CompassMiniTrip,
        AudioSystems = 12,
        TirePressureMonitor,
        MemorySeat,
        DoorMux,
        VehicleInfoCenter = 17,
        ECM,
        SKIM = 20,
        HeatVentAirConditioning,
        RightSideAirbag = 27,
        LeftSideAirbag,
        PowerLiftgateModule,
        PassengerSlidingDoor,
        DriverSlidingDoor,
        FCM_IPM,
        SRIM,
        Traveller,
        TransferCaseModule = 39,
        CabinHeaterModule = 41,
        TDR,
        DigitalAudioAmp = 44,
        RainSensorModule,
        AdjustablePedalModule,
        ShiftLevelSensorAssembly,
        SatelliteAudioAssembly,
        NAV,
        HandsFreeModule,
        DriverDoorModule,
        PassengerDoorModule,
        OccupantClassificationModule,
        ParkAssistModule = 55,
        FinalDriveControlModule,
        CentralGateway
    }
}
