'tabs=4
' --------------------------------------------------------------------------------
' TODO fill in this information for your driver, then remove this line!
'
' ASCOM ObservingConditions driver for NodeMCUWeatherStation
'
' Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
'				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
'				erat, sed diam voluptua. At vero eos et accusam et justo duo 
'				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
'				sanctus est Lorem ipsum dolor sit amet.
'
' Implements:	ASCOM ObservingConditions interface version: 1.0
' Author:		(XXX) Your N. Here <your@email.here>
'
' Edit Log:
'
' Date			Who	Vers	Description
' -----------	---	-----	-------------------------------------------------------
' dd-mmm-yyyy	XXX	1.0.0	Initial edit, from ObservingConditions template
' ---------------------------------------------------------------------------------
'
'
' Your driver's ID is ASCOM.NodeMCUWeatherStation.ObservingConditions
'
' The Guid attribute sets the CLSID for ASCOM.DeviceName.ObservingConditions
' The ClassInterface/None addribute prevents an empty interface called
' _ObservingConditions from being created and used as the [default] interface
'

' This definition is used to select code that's only applicable for one device type
#Const Device = "ObservingConditions"

Imports ASCOM
Imports ASCOM.Astrometry
Imports ASCOM.Astrometry.AstroUtils
Imports ASCOM.DeviceInterface
Imports ASCOM.Utilities

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text

<Guid("039b155e-1349-45ba-8ff2-71995535ba0c")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ObservingConditions

    ' The Guid attribute sets the CLSID for ASCOM.NodeMCUWeatherStation.ObservingConditions
    ' The ClassInterface/None addribute prevents an empty interface called
    ' _NodeMCUWeatherStation from being created and used as the [default] interface

    ' TODO Replace the not implemented exceptions with code to implement the function or
    ' throw the appropriate ASCOM exception.
    '
    Implements IObservingConditions

    '
    ' Driver ID and descriptive string that shows in the Chooser
    '
    Friend Shared driverID As String = "ASCOM.NodeMCUWeatherStation.ObservingConditions"
    Private Shared driverDescription As String = "NodeMCUWeatherStation ObservingConditions"

    Friend Shared comPortProfileName As String = "COM Port" 'Constants used for Profile persistence
    Friend Shared traceStateProfileName As String = "Trace Level"
    Friend Shared comPortDefault As String = "COM1"
    Friend Shared traceStateDefault As String = "False"

    Friend Shared comPort As String ' Variables to hold the currrent device configuration
    Friend Shared traceState As Boolean

    Private connectedState As Boolean ' Private variable to hold the connected state
    Private utilities As Util ' Private variable to hold an ASCOM Utilities object
    Private astroUtilities As AstroUtils ' Private variable to hold an AstroUtils object to provide the Range method
    Private TL As TraceLogger ' Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
    Private objSerial As ASCOM.Utilities.Serial
    Dim sDelimStart As String = "!#" 'First delimiting word
    Dim sDelimEnd As String = "#!" 'Second delimiting word


    '
    ' Constructor - Must be public for COM registration!
    '
    Public Sub New()

        ReadProfile() ' Read device configuration from the ASCOM Profile store
        TL = New TraceLogger("", "NodeMCUWeatherStation")
        TL.Enabled = traceState
        TL.LogMessage("ObservingConditions", "Starting initialisation")

        connectedState = False ' Initialise connected to false
        utilities = New Util() ' Initialise util object
        astroUtilities = New AstroUtils 'Initialise new astro utiliites object

        'TODO: Implement your additional construction here

        TL.LogMessage("ObservingConditions", "Completed initialisation")
    End Sub

    '
    ' PUBLIC COM INTERFACE IObservingConditions IMPLEMENTATION
    '

#Region "Common properties and methods"
    ''' <summary>
    ''' Displays the Setup Dialog form.
    ''' If the user clicks the OK button to dismiss the form, then
    ''' the new settings are saved, otherwise the old values are reloaded.
    ''' THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
    ''' </summary>
    Public Sub SetupDialog() Implements IObservingConditions.SetupDialog
        ' consider only showing the setup dialog if not connected
        ' or call a different dialog if connected
        If IsConnected Then
            System.Windows.Forms.MessageBox.Show("Already connected, just press OK")
        End If

        Using F As SetupDialogForm = New SetupDialogForm()
            Dim result As System.Windows.Forms.DialogResult = F.ShowDialog()
            If result = DialogResult.OK Then
                WriteProfile() ' Persist device configuration values to the ASCOM Profile store
            End If
        End Using
    End Sub

    Public ReadOnly Property SupportedActions() As ArrayList Implements IObservingConditions.SupportedActions
        Get
            TL.LogMessage("SupportedActions Get", "Returning empty arraylist")
            Return New ArrayList()
        End Get
    End Property

    Public Function Action(ByVal ActionName As String, ByVal ActionParameters As String) As String Implements IObservingConditions.Action
        Throw New ActionNotImplementedException("Action " & ActionName & " is not supported by this driver")
    End Function

    Public Sub CommandBlind(ByVal Command As String, Optional ByVal Raw As Boolean = False) Implements IObservingConditions.CommandBlind
        CheckConnected("CommandBlind")
        ' Call CommandString and return as soon as it finishes
        Me.CommandString(Command, Raw)
        ' or
        Throw New MethodNotImplementedException("CommandBlind")
    End Sub

    Public Function CommandBool(ByVal Command As String, Optional ByVal Raw As Boolean = False) As Boolean _
        Implements IObservingConditions.CommandBool
        CheckConnected("CommandBool")
        Dim ret As String = CommandString(Command, Raw)
        ' TODO decode the return string and return true or false
        ' or
        Throw New MethodNotImplementedException("CommandBool")
    End Function

    Public Function CommandString(ByVal Command As String, Optional ByVal Raw As Boolean = False) As String _
        Implements IObservingConditions.CommandString
        CheckConnected("CommandString")
        ' it's a good idea to put all the low level communication with the device here,
        ' then all communication calls this function
        ' you need something to ensure that only one command is in progress at a time
        Throw New MethodNotImplementedException("CommandString")
    End Function

    Public Property Connected() As Boolean Implements IObservingConditions.Connected
        Get
            TL.LogMessage("Connected Get", IsConnected.ToString())
            Return IsConnected
        End Get
        Set(value As Boolean)
            TL.LogMessage("Connected Set", value.ToString())
            If value = IsConnected Then
                Return
            End If

            If value Then
                connectedState = True

                Dim com As Integer
                com = Convert.ToInt32(comPort.Replace("COM", ""))

                TL.LogMessage("Connected Set", "Connecting to port " + comPort + " parsed as " + com.ToString)

                objSerial = New ASCOM.Utilities.Serial
                objSerial.Port = com
                objSerial.Speed = 57600
                objSerial.Connected = True
            Else
                connectedState = False
                TL.LogMessage("Connected Set", "Disconnecting from port " + comPort)
                objSerial.Connected = False
                objSerial.Dispose() 'Release serial port resources
                objSerial = Nothing
            End If
        End Set
    End Property

    Public ReadOnly Property Description As String Implements IObservingConditions.Description
        Get
            ' this pattern seems to be needed to allow a public property to return a private field
            Dim d As String = driverDescription
            TL.LogMessage("Description Get", d)
            Return d
        End Get
    End Property

    Public ReadOnly Property DriverInfo As String Implements IObservingConditions.DriverInfo
        Get
            Dim m_version As Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            ' TODO customise this driver description
            Dim s_driverInfo As String = "Driver to connect to a NODEMCU via USB Cable. Version: " + m_version.Major.ToString() + "." + m_version.Minor.ToString()
            TL.LogMessage("DriverInfo Get", s_driverInfo)
            Return s_driverInfo
        End Get
    End Property

    Public ReadOnly Property DriverVersion() As String Implements IObservingConditions.DriverVersion
        Get
            ' Get our own assembly and report its version number
            TL.LogMessage("DriverVersion Get", Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2))
            Return Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2)
        End Get
    End Property

    Public ReadOnly Property InterfaceVersion() As Short Implements IObservingConditions.InterfaceVersion
        Get
            TL.LogMessage("InterfaceVersion Get", "1")
            Return 1
        End Get
    End Property

    Public ReadOnly Property Name As String Implements IObservingConditions.Name
        Get
            Dim s_name As String = "NodeMCU Weather Station"
            TL.LogMessage("Name Get", s_name)
            Return s_name
        End Get
    End Property

    Public Sub Dispose() Implements IObservingConditions.Dispose
        ' Clean up the tracelogger and util objects
        TL.Enabled = False
        TL.Dispose()
        TL = Nothing
        utilities.Dispose()
        utilities = Nothing
        astroUtilities.Dispose()
        astroUtilities = Nothing
    End Sub

#End Region

#Region "IObservingConditions Implementation"

    Public Property AveragePeriod() As Double Implements IObservingConditions.AveragePeriod
        Get
            TL.LogMessage("AveragePeriod", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("AveragePeriod", False)
        End Get
        Set(value As Double)
            TL.LogMessage("AveragePeriod", "Set Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("AveragePeriod", True)
        End Set
    End Property

    Public ReadOnly Property CloudCover() As Double Implements IObservingConditions.CloudCover
        Get
            TL.LogMessage("CloudCover", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("CloudCover", False)
        End Get
    End Property

    Public ReadOnly Property DewPoint() As Double Implements IObservingConditions.DewPoint
        Get
            TL.LogMessage("DewPoint", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("DewPoint", False)
        End Get
    End Property

    Public ReadOnly Property Humidity() As Double Implements IObservingConditions.Humidity
        Get
            Dim hum As Double
            TL.LogMessage("Humidity", "Getting Humidity")
            objSerial.Transmit("GETHUMIDITY#")
            Dim sSource As String
            sSource = objSerial.ReceiveTerminated(sDelimEnd)

            hum = Double.Parse(ParseSerialResponse(sSource), CultureInfo.InvariantCulture)
            TL.LogMessage("Converted Humidity", "Humidity " + hum.ToString)
            Return hum

        End Get
    End Property

    Public ReadOnly Property Pressure() As Double Implements IObservingConditions.Pressure
        Get
            TL.LogMessage("Pressure", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("Pressure", False)
        End Get
    End Property

    Public ReadOnly Property RainRate() As Double Implements IObservingConditions.RainRate
        Get

            Dim rain As Double
            TL.LogMessage("RainRate", "Getting RainRate")
            objSerial.Transmit("GETRAINSTATE#")
            Dim sSource As String
            sSource = objSerial.ReceiveTerminated(sDelimEnd)

            rain = Double.Parse(ParseSerialResponse(sSource), CultureInfo.InvariantCulture)
            TL.LogMessage("Converted RainRate", "RainRate " + rain.ToString)
            Return rain

        End Get
    End Property

    Public ReadOnly Property SkyBrightness() As Double Implements IObservingConditions.SkyBrightness
        Get
            TL.LogMessage("SkyBrightness", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("SkyBrightness", False)
        End Get
    End Property

    Public ReadOnly Property SkyQuality() As Double Implements IObservingConditions.SkyQuality
        Get
            TL.LogMessage("SkyQuality", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("SkyQuality", False)
        End Get
    End Property

    Public ReadOnly Property StarFWHM() As Double Implements IObservingConditions.StarFWHM
        Get
            TL.LogMessage("StarFWHM", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("StarFWHM", False)
        End Get
    End Property

    Public ReadOnly Property SkyTemperature() As Double Implements IObservingConditions.SkyTemperature
        Get
            TL.LogMessage("SkyTemperature", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("SkyTemperature", False)
        End Get
    End Property

    Public ReadOnly Property Temperature() As Double Implements IObservingConditions.Temperature
        Get
            Dim temp As Double
            TL.LogMessage("Temperature", "Getting Temperature")
            objSerial.Transmit("GETTEMPERATURE#")
            Dim sSource As String
            sSource = objSerial.ReceiveTerminated(sDelimEnd)


            temp = Double.Parse(ParseSerialResponse(sSource), CultureInfo.InvariantCulture)

            TL.LogMessage("Converted Humidity", "Humidity " + temp.ToString)
            Return temp
        End Get
    End Property

    Public ReadOnly Property WindDirection() As Double Implements IObservingConditions.WindDirection
        Get
            TL.LogMessage("WindDirection", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("WindDirection", False)
        End Get
    End Property

    Public ReadOnly Property WindGust() As Double Implements IObservingConditions.WindGust
        Get
            TL.LogMessage("WindGust", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("WindGust", False)
        End Get
    End Property

    Public ReadOnly Property WindSpeed() As Double Implements IObservingConditions.WindSpeed
        Get
            TL.LogMessage("WindSpeed", "Get Not implemented")
            Throw New ASCOM.PropertyNotImplementedException("WindSpeed", False)
        End Get
    End Property

    Public Function TimeSinceLastUpdate(PropertyName As String) As Double Implements IObservingConditions.TimeSinceLastUpdate
        TL.LogMessage("TimeSinceLastUpdate", "Get Not implemented")
        Throw New ASCOM.MethodNotImplementedException("TimeSinceLastUpdate")
    End Function

    Public Function SensorDescription(PropertyName As String) As String Implements IObservingConditions.SensorDescription
        Select Case PropertyName.Trim.ToLowerInvariant
            Case "averageperiod"
                Return "Average period in hours, immediate values are only available"
            Case "dewpoint"
            Case "humidity"
            Case "pressure"
            Case "rainrate"
            Case "skybrightness"
            Case "skyquality"
            Case "starfwhm"
            Case "skytemperature"
            Case "temperature"
            Case "winddirection"
            Case "windgust"
            Case "windspeed"
                TL.LogMessage("SensorDescription", PropertyName & " - not implemented")
                Throw New MethodNotImplementedException("SensorDescription(" + PropertyName + ")")
        End Select
        TL.LogMessage("SensorDescription", PropertyName & " - unrecognised")
        Throw New ASCOM.InvalidValueException("SensorDescription(" + PropertyName + ")")
    End Function

    Public Sub Refresh() Implements IObservingConditions.Refresh
        TL.LogMessage("Refresh", "Not implemented")
        Throw New ASCOM.MethodNotImplementedException("Refresh")
    End Sub

#End Region

#Region "Private properties and methods"
    ' here are some useful properties and methods that can be used as required
    ' to help with

#Region "ASCOM Registration"

    Private Shared Sub RegUnregASCOM(ByVal bRegister As Boolean)

        Using P As New Profile() With {.DeviceType = "ObservingConditions"}
            If bRegister Then
                P.Register(driverID, driverDescription)
            Else
                P.Unregister(driverID)
            End If
        End Using

    End Sub

    <ComRegisterFunction()>
    Public Shared Sub RegisterASCOM(ByVal T As Type)

        RegUnregASCOM(True)

    End Sub

    <ComUnregisterFunction()>
    Public Shared Sub UnregisterASCOM(ByVal T As Type)

        RegUnregASCOM(False)

    End Sub

#End Region

    ''' <summary>
    ''' Returns true if there is a valid connection to the driver hardware
    ''' </summary>
    Private ReadOnly Property IsConnected As Boolean
        Get
            ' TODO check that the driver hardware connection exists and is connected to the hardware
            Return connectedState
        End Get
    End Property

    ''' <summary>
    ''' Use this function to throw an exception if we aren't connected to the hardware
    ''' </summary>
    ''' <param name="message"></param>
    Private Sub CheckConnected(ByVal message As String)
        If Not IsConnected Then
            Throw New NotConnectedException(message)
        End If
    End Sub

    Private Function ParseSerialResponse(ByVal sSource As String) As String

        Dim nIndexStart As Integer = sSource.IndexOf(sDelimStart)
        Dim nIndexEnd As Integer = sSource.IndexOf(sDelimEnd)

        TL.LogMessage("Received Serial Response", sSource)

        If nIndexStart > -1 AndAlso nIndexEnd > -1 Then '-1 means the word was not found.
            Dim res As String = Strings.Mid(sSource, nIndexStart + sDelimStart.Length + 1, nIndexEnd - nIndexStart - sDelimStart.Length) 'Crop the text between
            TL.LogMessage("Extracted Serial Response", res)
            Return res
        Else
            Return ""
        End If
    End Function

    ''' <summary>
    ''' Read the device configuration from the ASCOM Profile store
    ''' </summary>
    Friend Sub ReadProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "ObservingConditions"
            traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, String.Empty, traceStateDefault))
            comPort = driverProfile.GetValue(driverID, comPortProfileName, String.Empty, comPortDefault)
        End Using
    End Sub

    ''' <summary>
    ''' Write the device configuration to the  ASCOM  Profile store
    ''' </summary>
    Friend Sub WriteProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "ObservingConditions"
            driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString())
            driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString())
        End Using

    End Sub

#End Region

End Class
