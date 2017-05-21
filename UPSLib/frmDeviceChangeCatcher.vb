Imports UPSLib
Imports UPSLib.Win32
Imports UPSLib.DeviceChangeMessages
Imports UPSLib.Win32.WindowsMessages

Friend Class frmDeviceChangeCatcher

    Friend Sub New()
        InitializeComponent()

    End Sub

    Protected Overrides Sub WndProc(ByRef Message As System.Windows.Forms.Message)
        Select Case Message.Msg
            Case WM_DEVICECHANGE
                Select Case Message.WParam
                    Case DBT_DEVICEREMOVECOMPLETE, DBT_DEVICEARRIVAL
                        Dim DeviceInfo As DEV_BROADCAST_HDR = DirectCast(Marshal.PtrToStructure(Message.LParam, GetType(DEV_BROADCAST_HDR)), DEV_BROADCAST_HDR)
                        If DeviceInfo.dbch_DeviceType = DBT_DEVTYP_DEVICEINTERFACE Then
                            UPSLibController.Instance().SignalDeviceChange()
                        End If
                    Case DBT_DEVNODES_CHANGED
                        UPSLibController.Instance().SignalDeviceChange()
                End Select
        End Select
        MyBase.WndProc(Message)
    End Sub

End Class