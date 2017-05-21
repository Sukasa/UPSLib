Imports UPSLib.Win32
Imports Microsoft.Win32.SafeHandles
Imports System.Drawing.Imaging
Imports System.Threading
Imports System.Windows.Forms
Imports System.Resources
Imports System.Globalization

Namespace UPSLib
    Public MustInherit Class CompatibleUPS

        Private HandleOpen As Boolean = True

        Friend Shared HardwareInterface As UPSLib.HardwareInterface

        Friend DevicePath As String
        Friend ReadHandle As Integer

        Protected MustOverride Sub InterpretReport(ByVal Code As Byte())

        Protected UPSData As New UPSData

        Public ReadOnly Property Connected() As Boolean
            Get
                Return HandleOpen
            End Get
        End Property

        Public ReadOnly Property UPSReadings() As UPSData
            Get
                Return UPSData
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Me.GetType.Name
        End Function

        Public ReadOnly Property UPSPath() As String
            Get
                Return DevicePath
            End Get
        End Property

        Friend Overridable Sub PollForUpdates()

        End Sub

        Friend Sub Init(ByVal DeviceID As String, ByVal IsNew As Boolean)
            DevicePath = DeviceID
            Console.WriteLine("Initing device at path " & DeviceID)

            ReadHandle = HardwareInterface.OpenInterface(DeviceID)

            HandleOpen = ReadHandle > 0

            If HandleOpen Then
                Console.WriteLine("Init Success")
            Else
                Console.WriteLine("Init Fail")
            End If

            If Not HandleOpen Then Return
            CustomInit()
        End Sub

        Friend Sub Close()
            HandleOpen = False
            HardwareInterface.CloseInterface(ReadHandle)
        End Sub

        Protected Overridable Sub CustomInit()

        End Sub
    End Class
End Namespace