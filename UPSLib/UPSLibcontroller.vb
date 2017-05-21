Imports UPSLib.Win32
Imports System.IO
Imports System.Threading

Namespace UPSLib
    Public Class UPSLibController
        Private Shared UPSInterface As New UPSLib.HardwareInterface

        Private KeyboardHandle As Integer
        Private Shared DeviceChangeNotifier As frmDeviceChangeCatcher
        Private CachedUPSList As New List(Of CompatibleUPS)

        Public Delegate Sub DeviceChangeEventHandler()

        Public Event DeviceChange As DeviceChangeEventHandler

        Private Shared SingletonInstance As UPSLibController
        Private Shared PollThread As Thread
        Private Shared IsReloading As Boolean = False

        Public Event PollComplete As Action(Of UPSLibController)

        Private Sub New()
            UPSInterface.LocateHardware()
            PollThread = New Thread(AddressOf ThreadedPollUPSes)
            PollThread.Start()
        End Sub

        Public Shared Function Instance() As UPSLibController
            If SingletonInstance Is Nothing Then
                SingletonInstance = New UPSLibController()
            End If
            Return SingletonInstance
        End Function

        Private Sub ThreadedPollUPSes()

            DeviceChangeNotifier = New frmDeviceChangeCatcher()
            DeviceChangeNotifier.Show() ' Show then hide, so that the form starts capturing events (Is there a less hacky way of doing this?)
            DeviceChangeNotifier.Hide()

            While True
                Thread.Sleep(1000)

                System.Windows.Forms.Application.DoEvents() ' Process events, so we can catch device changes

                If IsReloading Then Continue While

                For Each UPS As CompatibleUPS In UPSes
                    If UPS.Connected Then UPS.PollForUpdates()
                Next

                RaiseEvent PollComplete(Me)

            End While
        End Sub

        Public ReadOnly Property UPSes() As List(Of CompatibleUPS)
            Get
                If CachedUPSList.Count = 0 Then
                    If UPSInterface.CurrentUPSes.Count = 0 Then
                        IsReloading = True
                        UPSInterface.LocateHardware()
                        IsReloading = False
                    End If
                    Dim List As New List(Of CompatibleUPS)(UPSInterface.CurrentUPSes)
                    List.RemoveAll(Function(T) Not T.Connected) 'Apply Filter
                    CachedUPSList = List 'And cache results
                End If
                Dim TempList As New List(Of CompatibleUPS)(CachedUPSList.Count)
                TempList.AddRange(CachedUPSList)
                Return TempList ' Don't return the cached list, so that client code can't accidentally modify it
            End Get
        End Property

        Friend Sub SignalDeviceChange()
            IsReloading = True
            CachedUPSList.Clear() 'Clear filtered-UPSes cache
            UPSInterface.LocateHardware()
            RaiseEvent DeviceChange()
            IsReloading = False
        End Sub
    End Class
End Namespace
