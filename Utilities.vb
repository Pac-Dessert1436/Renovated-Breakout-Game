Imports VbPixelGameEngine
Imports System.MathF

Friend Class RectF
    Public Property X As Single
    Public Property Y As Single
    Public Property Width As Single
    Public Property Height As Single

    Public Sub New(x As Single, y As Single, w As Single, h As Single)
        Me.X = x
        Me.Y = y
        Width = w
        Height = h
    End Sub

    Public Function IsCollidingWith(target As RectF) As Boolean
        Return Not (X + Width < target.X OrElse
                    X > target.X + target.Width OrElse
                    Y + Height < target.Y OrElse
                    Y > target.Y + target.Height)
    End Function
End Class

Friend Class Block
    Public Property Rect As RectF
    Public Property Pixel As Pixel
    Public Property IsActive As Boolean = True

    Public Sub New(pixel As Pixel, x As Integer, y As Integer, width As Integer, height As Integer)
        Me.Pixel = pixel
        Rect = New RectF(x, y, width, height)
    End Sub

    Public Sub New(pixel As Pixel, width As Integer, height As Integer)
        Me.Pixel = pixel
        Rect = New RectF(0, 0, width, height)
    End Sub

    Public Sub Draw(pge As PixelGameEngine)
        If IsActive Then pge.FillRect(Rect.X, Rect.Y, Rect.Width, Rect.Height, Pixel)
    End Sub
End Class

Friend Class Paddle
    Inherits Block

    Public Property MoveSpeed As Single

    Public Sub New(pixel As Pixel, width As Integer, height As Integer)
        MyBase.New(pixel, width, height)
    End Sub

    Public Sub ClampToField(fieldLeft As Integer, fieldRight As Integer)
        Rect.X = Max(fieldLeft, Min(Rect.X, fieldRight - Rect.Width))
    End Sub
End Class

Friend Class Ball
    Inherits Block

    Public Property Velocity As Vf2d
    Public Const MOVE_SPEED As Single = 1.2F

    Public Sub New(pixel As Pixel, size As Integer)
        MyBase.New(pixel, size, size)
        Randomize()
    End Sub

    Public Sub UpdatePosition()
        Rect.X += Velocity.x
        Rect.Y += Velocity.y

        If Abs(Velocity.x) < 1 Then Velocity = New Vf2d(If(Rnd() > 0.5, 1, -1), Velocity.y)
    End Sub

    Public Sub BounceX()
        Velocity = New Vf2d(-Velocity.x, Velocity.y)
    End Sub

    Public Sub BounceY()
        Velocity = New Vf2d(Velocity.x, -Velocity.y)
    End Sub

    Public Sub ResetToPaddle(paddle As Paddle)
        Rect.X = paddle.Rect.X + (paddle.Rect.Width / 2) - (Rect.Width / 2)
        Rect.Y = paddle.Rect.Y - Rect.Height - 2
        Velocity = New Vf2d(If(Rnd() > 0.5, MOVE_SPEED, -MOVE_SPEED), -MOVE_SPEED)
    End Sub
End Class

Friend Enum GameState
    Attraction = 0
    Playing = 1
    Paused = 2
    GameOver = 3
    [Continue] = 4
End Enum