Imports UPSLib.Win32
Imports System.Security.AccessControl

Namespace UPSLib
    Friend Class HardwareInterface
        Friend CurrentUPSes As New List(Of CompatibleUPS)

        Private PreviousUPSes As New List(Of CompatibleUPS)
        Private UPSes As Dictionary(Of String, Type) = New Dictionary(Of String, Type)

        Friend Sub New()
            CompatibleUPS.HardwareInterface = Me
            InitDictionary()
        End Sub

        Public Function GetKeyInfo(ByVal KeyboardPath As String) As Object

            Dim KeyboardHandle As Integer = OpenInterface(KeyboardPath)
            Dim BytesRead As Integer
            Dim Buffer(6) As Byte
            ReadFile(KeyboardHandle, Buffer, Buffer.Length, BytesRead, Nothing)

            CloseInterface(KeyboardHandle)
            Return Buffer
        End Function

        Friend Shared Function OpenInterface(ByVal DevicePath As String) As Integer

            Dim SecurityData As New SECURITY_ATTRIBUTES()
            Dim Security As New DirectorySecurity()
            Dim DescriptorBinary As Byte() = Security.GetSecurityDescriptorBinaryForm()
            Dim SecurityDescriptorPtr As IntPtr = Marshal.AllocHGlobal(DescriptorBinary.Length)

            SecurityData.nLength = Marshal.SizeOf(SecurityData)
            Marshal.Copy(DescriptorBinary, 0, SecurityDescriptorPtr, DescriptorBinary.Length)
            SecurityData.lpSecurityDescriptor = SecurityDescriptorPtr

            Dim Handle As Integer = Win32.CreateFile(DevicePath, GENERIC_READ Or GENERIC_WRITE, _
                                                     FILE_SHARE_READ Or FILE_SHARE_WRITE, SecurityData, _
                                                     OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0)

            Marshal.FreeHGlobal(SecurityDescriptorPtr)

            Return Handle
        End Function

        Friend Shared Sub CloseInterface(ByRef fileHandle As Integer)
            Win32.CloseHandle(fileHandle)
        End Sub

        Friend Sub LocateHardware()
            PreviousUPSes.AddRange(CurrentUPSes.FindAll(Function(T) Not PreviousUPSes.Contains(T)))
            'MultiPartResolver.Instance.ClearPartsList()

            Console.WriteLine("Hardware Scan")

            For Each UPS As CompatibleUPS In CurrentUPSes
                UPS.Close()
            Next

            CurrentUPSes = New List(Of CompatibleUPS)

            Dim HIDGuid As Guid = Guid.Empty
            Win32.HidD_GetHidGuid(HIDGuid)

            Dim ConnectedDevices As IntPtr = Win32.SetupDiGetClassDevs(HIDGuid, Nothing, 0, DIGCF_DEVICEINTERFACE Or DIGCF_PRESENT)

            Dim DeviceIndex As Integer = 0
            Dim InterfaceInfo As SP_DEVICE_INTERFACE_DATA
            InterfaceInfo.cbSize = Marshal.SizeOf(InterfaceInfo)

            While Win32.SetupDiEnumDeviceInterfaces(ConnectedDevices, 0, HIDGuid, DeviceIndex, InterfaceInfo)

                Dim RequiredDataSize As Integer

                Dim DeviceInfo As New SP_DEVINFO_DATA
                DeviceInfo.cbSize = Marshal.SizeOf(DeviceInfo)

                Win32.SetupDiGetDeviceInterfaceDetail(ConnectedDevices, InterfaceInfo, IntPtr.Zero, 0, RequiredDataSize, DeviceInfo)

                Dim MemPtr As IntPtr = Marshal.AllocHGlobal(RequiredDataSize)

                'Okay, this is pretty hacky but I really don't know of a better way to write this code.
                'If the binary is compiled as x64, I have to declare the size of the struct as
                '8 bytes instead of 6 due to packing (8 byte vs. 1 byte)
                Marshal.WriteInt32(MemPtr, If(IntPtr.Size = 8, 8, 4 + Marshal.SystemDefaultCharSize))

                Win32.SetupDiGetDeviceInterfaceDetail(ConnectedDevices, InterfaceInfo, MemPtr, RequiredDataSize, RequiredDataSize, DeviceInfo)

                Dim DeviceCode As New IntPtr(MemPtr.ToInt32 + 4) ' 4 = System.Int32.Size
                Dim DeviceString As String = Marshal.PtrToStringAuto(DeviceCode)

                'MultiPartResolver.Instance.AddPath(DeviceString, DeviceInfo.DevInst)

                Dim UPS As CompatibleUPS = Nothing
                For I As Integer = 0 To UPSes.Keys.Count - 1
                    If (DeviceString.IndexOf(UPSes.Keys(I)) > 0) Then
                        UPS = PreviousUPSes.Find(Function(T) T.DevicePath = DeviceString)
                        If UPS Is Nothing Then
                            UPS = Activator.CreateInstance(UPSes.Values(I), True)
                            UPS.Init(DeviceString, True)
                        Else
                            UPS.Init(DeviceString, False)
                        End If
                        CurrentUPSes.Add(UPS)
                        'If Keyboard.GetType IsNot GetType(KeyboardMediaEndpoint) Then ' Registering a KME can cause it to find ITSELF as master, leading to an infinite recursive loop and stack overflow...
                        '     MultiPartResolver.Instance.RegisterConnectedKeyboard(Keyboard) ' ...So don't.
                        'End If
                        Exit For
                    End If
                Next I

                Marshal.FreeHGlobal(MemPtr)
                DeviceIndex += 1
            End While
            Win32.SetupDiDestroyDeviceInfoList(ConnectedDevices)

        End Sub

        Public Shared Function GetParentDeviceID(ByVal DeviceHandle As Integer) As String
            Dim Previous As Integer = Nothing

            CM_Get_Parent(Previous, DeviceHandle, 0)

            Return Previous
        End Function

        Public Shared Function GetDevicePathFromHandle(ByVal DeviceHandle As UInteger) As String
            Dim Bytes As Integer = 0
            CM_Get_Device_ID_Size(Bytes, DeviceHandle, 0)
            Dim StrPtr As IntPtr = Marshal.AllocHGlobal((Bytes + 1) * Marshal.SystemDefaultCharSize)

            CM_Get_Device_ID(DeviceHandle, StrPtr, Bytes, 0)

            Dim DevicePath As String = Marshal.PtrToStringAuto(StrPtr, Bytes)

            Marshal.FreeHGlobal(StrPtr)

            Return DevicePath
        End Function
        Friend Function GetFeature(ByVal UPS As CompatibleUPS, ByRef FeatureData As Byte()) As Byte()
            If FeatureData Is Nothing OrElse Not UPS.Connected Then Return Nothing
            Win32.HidD_GetFeature(UPS.ReadHandle, FeatureData, FeatureData.Length)
            Return FeatureData
        End Function

        Private Sub InitDictionary()

            'Don't forget to add the necessary paths to MultiPartResolver.vb (@ top of file)

            UPSes.Add("vid_0764&pid_0501", GetType(CyberpowerUPS))
            UPSes.Add("vid_0764&pid_0601", GetType(CyberpowerUPS))
        End Sub

    End Class
End Namespace

