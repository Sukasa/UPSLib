Namespace UPSLib
    Public Class CyberpowerUPS
        Inherits CompatibleUPS

        Protected Overrides Sub InterpretReport(ByVal Code() As Byte)
            If Code Is Nothing OrElse Code.Length < 1 Then Return
            Console.Write("DEBUG: ")

            For X As Integer = 0 To Code.Length - 1
                Console.Write(Code(X).ToString("X") + " ")
            Next
            Console.WriteLine("")

            Select Case Code(0)
                Case &H8
                    UPSData.Battery.ChargePercentage = Code(1)
                    UPSData.Battery.RuntimeSeconds = (CInt(Code(3)) << 8) + Code(2)
                    UPSData.TimeToShutdownSeconds = (CInt(Code(5)) << 8) + Code(4)
                Case &HB
                    UPSData.Battery.BatteryStatus = BatteryStatus.NotConnected
                    If (Code(1) And 2) = 2 Then
                        UPSData.Battery.BatteryStatus = BatteryStatus.Charging
                    ElseIf (Code(1) And 4) = 4 Then
                        UPSData.Battery.BatteryStatus = BatteryStatus.Discharging
                    ElseIf (Code(1) And 16) = &H10 Then
                        UPSData.Battery.BatteryStatus = BatteryStatus.Charged
                    End If
                    UPSData.ACPresent = (Code(1) And 1) = 1
                Case &HF
                    UPSData.LineVoltage = Code(1)
                Case &H10
                    UPSData.HighVoltageThreshold = Code(2)
                    UPSData.LowVoltageThreshold = Code(1)
                Case &H12
                    UPSData.OutputVoltage = Code(1)
                Case &H13
                    UPSData.InverterLoadPercentage = Code(1)
                Case &H18
                    UPSData.InverterWattage = (CInt(Code(2)) << 8) + Code(1)
                Case &H7

            End Select
        End Sub

        Protected Overrides Sub CustomInit()
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&H18, 0, 0}))
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&H10, 0, 0, 0, 0, 0, 0}))
        End Sub

        Friend Overrides Sub PollForUpdates()
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&H8, 0, 0, 0, 0, 0, 0}))
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&HB, 0}))
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&HF, 0}))
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&H12, 0}))
            InterpretReport(HardwareInterface.GetFeature(Me, New Byte() {&H13, 0}))
        End Sub
    End Class

End Namespace