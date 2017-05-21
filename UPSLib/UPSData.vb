Namespace UPSLib
    Public Structure UPSData

        Public InverterWattage As Integer
        Public InverterLoadPercentage As Byte

        Public Battery As BatteryInfo

        Public ACPresent As Boolean
        Public LineVoltage As Byte
        Public OutputVoltage As Byte
        Public TimeToShutdownSeconds As Integer

        Public LowVoltageThreshold As Byte
        Public HighVoltageThreshold As Byte

        Public Structure BatteryInfo
            Public ChargePercentage As Byte
            Public RuntimeSeconds As Integer
            Public LowBattery As Boolean

            Public BatteryStatus As BatteryStatus
        End Structure

        Public Function WattageLoad() As Integer
            Return (InverterLoadPercentage * InverterWattage) \ 100
        End Function

    End Structure

End Namespace