Imports NAudio.Wave

Public Class SoundPlayer
    Private ReadOnly reader As AudioFileReader
    Private ReadOnly waveOut As WaveOutEvent
    Private isLooping As Boolean = False

    Public Sub New(filename As String)
        reader = New AudioFileReader(filename)
        waveOut = New WaveOutEvent
        waveOut.Init(reader)

        AddHandler waveOut.PlaybackStopped, AddressOf OnPlaybackStopped
    End Sub

    Public Sub PlayOnce()
        If waveOut IsNot Nothing Then
            isLooping = False
            reader.Position = 0
            waveOut.Play()
        End If
    End Sub

    Public Sub PlayLooping()
        If waveOut IsNot Nothing Then
            isLooping = True
            reader.Position = 0
            waveOut.Play()
        End If
    End Sub

    Public Sub [Stop]()
        If waveOut IsNot Nothing Then
            isLooping = False
            waveOut.Stop()
        End If
    End Sub

    Public Sub OnPlaybackStopped(sender As Object, e As StoppedEventArgs)
        If isLooping AndAlso waveOut IsNot Nothing Then
            reader.Position = 0
            waveOut.Play()
        End If
    End Sub
End Class