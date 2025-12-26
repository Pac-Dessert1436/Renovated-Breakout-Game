Imports VbPixelGameEngine
Imports YamlDotNet.Serialization
Imports YamlDotNet.Serialization.NodeTypeResolvers
Imports System.MathF

Public NotInheritable Class Program
    Inherits PixelGameEngine

    Private ReadOnly Property Colors As New Dictionary(Of String, Pixel) From {
        {"Red", New Pixel(200, 72, 72)},
        {"Orange", New Pixel(198, 108, 58)},
        {"DarkYellow", New Pixel(180, 122, 48)},
        {"Yellow", New Pixel(162, 162, 42)},
        {"Green", New Pixel(72, 160, 72)},
        {"Blue", New Pixel(66, 72, 200)},
        {"Wall", New Pixel(142, 142, 142)},
        {"WallBottomLeft", New Pixel(66, 158, 130)},
        {"WallBottomRight", New Pixel(200, 72, 72)},
        {"Ball", New Pixel(105, 105, 105)},
        {"Paddle", New Pixel(91, 92, 214)}
    }

    Private Const BONUS_LIFE_EVERY As Integer = 192
    Private Const BLOCK_WIDTH As Integer = 16
    Private Const BLOCK_HEIGHT As Integer = 6
    Private Const WALL_THICKNESS As Integer = 16
    Private Const PADDLE_WIDTH As Integer = 24
    Private Const PADDLE_HEIGHT As Integer = 4
    Private Const BALL_SIZE As Integer = 4
    Private Const CONTINUE_TIMER_DURATION As Integer = 2

    Private ReadOnly m_blocks As New List(Of Block)
    Private ReadOnly m_walls As New List(Of Block)
    Private ReadOnly m_paddle As New Paddle(Colors("Paddle"), PADDLE_WIDTH, PADDLE_HEIGHT)
    Private ReadOnly m_ball As New Ball(Colors("Ball"), BALL_SIZE)

    Private m_gameState As GameState
    Private m_score As Integer
    Private m_lives As Integer
    Private m_level As Integer
    Private m_bonusLivesMult As Integer
    Private m_isStarting As Boolean
    Private m_prevGameData As (score As Integer, lives As Integer, level As Integer)

    Private m_fieldLeft As Integer
    Private m_fieldTop As Integer
    Private m_fieldRight As Integer
    Private m_paddleTopY As Integer

    Private m_ballTimer As Single
    Private m_ballMoveInterval As Single
    Private m_paddleInputTimer As Single
    Private m_continueTimer As Single
    Private m_continueCountdown As Integer

    Private ReadOnly sndBallBounce As New SoundPlayer("Assets/ball_bounce.wav")
    Private ReadOnly sndBlockBroken As New SoundPlayer("Assets/block_broken.wav")
    Private ReadOnly sndLifeGained As New SoundPlayer("Assets/life_gained.wav")
    Private ReadOnly sndLifeLost As New SoundPlayer("Assets/life_lost.wav")
    Private ReadOnly sndMainThemeIntro As New SoundPlayer("Assets/main_theme_intro.wav")
    Private ReadOnly bgmAttraction As New SoundPlayer("Assets/attraction.mp3")
    Private ReadOnly bgmMainThemeLoop As New SoundPlayer("Assets/main_theme_loop.mp3")
    Private ReadOnly bgmGameOver As New SoundPlayer("Assets/game_over.mp3")
    Private ReadOnly bgmContinue As New SoundPlayer("Assets/continue.mp3")

    Private Shared ReadOnly Property NumberPatterns As List(Of String)
        Get
            Return (New DeserializerBuilder) _
                .WithoutNodeTypeResolver(Of TagNodeTypeResolver)() _
                .Build().Deserialize(Of List(Of String))(IO.File.ReadAllText("Assets/NumberPatterns.yml"))
        End Get
    End Property
    Private ReadOnly Property TextColor As Pixel = Colors("Wall")

    Public Sub New()
        AppName = "Renovated Breakout Game"
    End Sub

    Protected Overrides Function OnUserCreate() As Boolean
        m_fieldTop = 2 * WALL_THICKNESS + 2
        m_fieldLeft = WALL_THICKNESS
        m_fieldRight = ScreenWidth - WALL_THICKNESS
        m_paddleTopY = ScreenHeight - (PADDLE_HEIGHT * 3)
        CreateWalls()
        ResetPaddleAndBall()
        CreateLevelBlocks()
        ResetGameState()
        Return True
    End Function

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        HandleInput(elapsedTime)
        Clear(Presets.Black)
        m_ballMoveInterval = Max(0.005F, 0.015F - (m_level - 1) * 0.002F)
        m_paddle.MoveSpeed = Min(1.5F + (m_level - 1) * 0.25F, 3.5F)

        Static gameOverTimer As Single = 0, mainThemeIntroTimer As Single = 0
        Const GAME_OVER_DURATION As Integer = 5
        Const MAIN_THEME_INTRO_DURATION As Single = 1.5F

        Select Case m_gameState
            Case GameState.Attraction
                If m_isStarting Then
                    m_prevGameData = (m_score, m_lives, m_level)
                    DrawString(90, 120, $"PLEASE GET READY!", TextColor)
                    mainThemeIntroTimer += elapsedTime
                    If mainThemeIntroTimer > MAIN_THEME_INTRO_DURATION Then
                        TransferToGameState(GameState.Playing)
                        mainThemeIntroTimer = 0
                    End If
                Else
                    DrawString(55, 40, $"* RENOVATED BREAKOUT GAME *", Colors("Orange"))
                    DrawString(50, 120, $"BONUS LIFE AT EVERY {BONUS_LIFE_EVERY} pts.", TextColor)
                    DrawString(75, 150, "PRESS ""ENTER"" TO START", TextColor)
                    UpdateAttractionMode(elapsedTime)
                End If
            Case GameState.Playing
                m_ballTimer += elapsedTime
                If m_ballTimer < m_ballMoveInterval Then Exit Select
                m_ballTimer -= m_ballMoveInterval
                m_ball.UpdatePosition()
                CheckCollisions()
                If IsLevelCompleted() Then NextLevel()
                If IsBallDropped() Then LoseLife()
            Case GameState.Paused
                DrawString(120, 120, "- PAUSED -", TextColor)
                DrawString(50, 150, "PRESS ""P"" AGAIN TO CONTINUE", TextColor)
            Case GameState.GameOver
                m_prevGameData = (m_score, m_lives, m_level)
                If m_level >= 9 AndAlso IsLevelCompleted() Then
                    DrawString(95, 120, "GAME COMPLETED", TextColor)
                    DrawString(75, 150, "Thanks for playing!", TextColor)
                Else
                    DrawString(120, 120, "GAME OVER", TextColor)
                    DrawString(75, 150, "Better luck next time!", TextColor)
                End If
                gameOverTimer += elapsedTime
                If gameOverTimer > GAME_OVER_DURATION Then
                    gameOverTimer = 0
                    m_gameState = GameState.Attraction
                    ResetGame()
                End If
            Case GameState.Continue
                DrawString(100, 120, $"CONTINUE? {m_continueCountdown}", Colors("Yellow"))
                DrawString(55, 140, "PRESS ""SPACE"" TO CONTINUE", TextColor)
                DrawString(25, 160, "Score is halved when you continue.", TextColor)
                DrawString(35, 175, "Hold ""ENTER"" to skip countdown.", TextColor)
                m_continueTimer += elapsedTime
                If m_continueTimer > CONTINUE_TIMER_DURATION OrElse GetKey(Key.ENTER).Pressed Then
                    m_continueTimer = 0
                    m_continueCountdown -= 1
                    If m_continueCountdown < 0 Then TransferToGameState(GameState.GameOver)
                End If
        End Select

        m_paddle.Draw(Me)
        m_ball.Draw(Me)
        For Each wall As Block In m_walls : wall.Draw(Me) : Next wall
        For Each block As Block In m_blocks : block.Draw(Me) : Next block
        If Not m_prevGameData.Equals((0, 0, 0)) AndAlso m_gameState = GameState.Attraction Then
            DrawHUD(m_prevGameData.score, m_prevGameData.lives, m_prevGameData.level)
        Else
            DrawHUD(m_score, m_lives, m_level)
        End If

        Return Not GetKey(Key.ESCAPE).Pressed
    End Function

    Private Sub HandleInput(dt As Single)
        m_paddleInputTimer += dt
        Dim inputInterval As Single = 0.005F
        Select Case m_gameState
            Case GameState.Attraction
                If GetKey(Key.ENTER).Pressed Then
                    ResetGame()
                    sndMainThemeIntro.PlayOnce()
                    m_isStarting = True
                End If
            Case GameState.Playing
                If GetKey(Key.P).Pressed Then m_gameState = GameState.Paused
                If GetKey(Key.LEFT).Held AndAlso m_paddleInputTimer > inputInterval Then
                    m_paddle.Rect.X -= m_paddle.MoveSpeed
                    m_paddleInputTimer = 0.0F
                End If
                If GetKey(Key.RIGHT).Held AndAlso m_paddleInputTimer > inputInterval Then
                    m_paddle.Rect.X += m_paddle.MoveSpeed
                    m_paddleInputTimer = 0.0F
                End If
                m_paddle.ClampToField(m_fieldLeft, m_fieldRight)
                If GetKey(Key.R).Pressed Then ResetGame()
            Case GameState.Paused
                If GetKey(Key.P).Pressed Then m_gameState = GameState.Playing
            Case GameState.Continue
                If GetKey(Key.SPACE).Pressed Then
                    m_continueCountdown = 9
                    m_score \= 2
                    m_lives = 3
                    m_bonusLivesMult = m_score \ BONUS_LIFE_EVERY + 1
                    ResetPaddleAndBall()
                    TransferToGameState(GameState.Playing)
                End If
        End Select
    End Sub

    Private Sub CheckCollisions()
        If m_ball.Rect.X <= m_fieldLeft OrElse m_ball.Rect.X + m_ball.Rect.Width >= m_fieldRight Then
            m_ball.BounceX()
            m_ball.Rect.X = Max(m_fieldLeft, Min(m_ball.Rect.X, m_fieldRight - m_ball.Rect.Width))
            If m_gameState = GameState.Playing Then sndBallBounce.PlayOnce()
        ElseIf m_ball.Rect.Y <= m_fieldTop Then
            m_ball.BounceY()
            m_ball.Rect.Y = m_fieldTop
            If m_gameState = GameState.Playing Then sndBallBounce.PlayOnce()
        End If
        If m_ball.Rect.IsCollidingWith(m_paddle.Rect) AndAlso m_ball.Velocity.y > 0 Then
            m_ball.BounceY()
            Dim hitOffset = m_ball.Rect.X + (m_ball.Rect.Width / 2) - (m_paddle.Rect.X + m_paddle.Rect.Width / 2)
            Const X_OFFSET_EXTENT As Single = 0.25F
            m_ball.Velocity = New Vf2d(hitOffset * X_OFFSET_EXTENT, m_ball.Velocity.y)
            Dim newVelX = Math.Clamp(m_ball.Velocity.x, -Ball.MOVE_SPEED, Ball.MOVE_SPEED)
            Dim newVelY = Math.Clamp(m_ball.Velocity.y, -Ball.MOVE_SPEED, Ball.MOVE_SPEED)
            m_ball.Velocity = New Vf2d(newVelX, newVelY)
            If m_gameState = GameState.Playing Then sndBallBounce.PlayOnce()
        End If
        For Each block As Block In m_blocks
            If Not block.IsActive Then Continue For
            If m_ball.Rect.IsCollidingWith(block.Rect) Then
                block.IsActive = False
                Dim overlapLeft = m_ball.Rect.X + m_ball.Rect.Width - block.Rect.X
                Dim overlapRight = block.Rect.X + block.Rect.Width - m_ball.Rect.X
                Dim overlapTop = m_ball.Rect.Y + m_ball.Rect.Height - block.Rect.Y
                Dim overlapBottom = block.Rect.Y + block.Rect.Height - m_ball.Rect.Y
                If Min(overlapLeft, overlapRight) < Min(overlapTop, overlapBottom) Then
                    m_ball.BounceX()
                    If overlapLeft < overlapRight Then
                        m_ball.Rect.X = block.Rect.X - m_ball.Rect.Width
                    Else
                        m_ball.Rect.X = block.Rect.X + block.Rect.Width
                    End If
                Else
                    m_ball.BounceY()
                    If overlapTop < overlapBottom Then
                        m_ball.Rect.Y = block.Rect.Y - m_ball.Rect.Height
                    Else
                        m_ball.Rect.Y = block.Rect.Y + block.Rect.Height
                    End If
                End If
                AddScore(GetBlockScore(block.Pixel))
                If m_gameState = GameState.Playing Then sndBlockBroken.PlayOnce()
                Exit For
            End If
        Next block
    End Sub

    Private Sub DrawHUD(score As Integer, lives As Integer, level As Integer)
        Dim offsetX As Integer = 8 : Const OFFSET_Y As Integer = 3
        DrawString(offsetX, OFFSET_Y, "SCORE", TextColor)
        offsetX += 48
        DrawNumberString(score.ToString("000").PadLeft(4), offsetX, OFFSET_Y)
        offsetX += 28 * 4
        DrawString(offsetX + 8, OFFSET_Y, "LIVES", TextColor)
        offsetX += 56
        DrawNumberString(lives.ToString(), offsetX, OFFSET_Y)
        offsetX += 28
        DrawString(offsetX + 8, OFFSET_Y, "LVL.", TextColor)
        offsetX += 40
        DrawNumberString(level.ToString(), offsetX, OFFSET_Y)
    End Sub

    Private Sub DrawNumberString(text As String, x As Integer, y As Integer)
        For Each ch As Char In text
            If Char.IsDigit(ch) Then
                Dim numIdx = Val(ch), color = Colors("Yellow")
                If numIdx < 0 OrElse numIdx >= NumberPatterns.Count Then Return
                Dim dotRows As String() = NumberPatterns(numIdx).Split(vbLf)
                For row As Integer = 0 To dotRows.Length - 1
                    If row >= 10 Then Exit For
                    For col As Integer = 0 To dotRows(row).Length - 1
                        If col >= 24 Then Exit For
                        If dotRows(row)(col) = "1"c Then Draw(x + col, y + row, color)
                    Next col
                Next row
            End If
            x += 28
        Next ch
    End Sub

    Private Sub CreateWalls()
        m_walls.Clear()
        m_walls.Add(New Block(Colors("Wall"), 0, WALL_THICKNESS, ScreenWidth, WALL_THICKNESS))
        m_walls.Add(New Block(Colors("Wall"), 0, WALL_THICKNESS, WALL_THICKNESS, ScreenHeight - WALL_THICKNESS))
        m_walls.Add(New Block(Colors("Wall"), ScreenWidth - WALL_THICKNESS, WALL_THICKNESS, WALL_THICKNESS, ScreenHeight - WALL_THICKNESS))
        m_walls.Add(New Block(Colors("WallBottomLeft"), 0, m_paddleTopY, WALL_THICKNESS, ScreenHeight - m_paddleTopY))
        m_walls.Add(New Block(Colors("WallBottomRight"), ScreenWidth - WALL_THICKNESS, m_paddleTopY, WALL_THICKNESS, ScreenHeight - m_paddleTopY))
    End Sub

    Private Sub CreateLevelBlocks()
        m_blocks.Clear()
        For row As Integer = 0 To 5
            Dim blockColor As Pixel = GetRowColor(row)
            For col As Integer = 0 To 17
                Dim blockX As Integer = m_fieldLeft + (col * BLOCK_WIDTH)
                Dim blockY As Integer = m_fieldTop + 26 + (row * BLOCK_HEIGHT)
                m_blocks.Add(New Block(blockColor, blockX, blockY, BLOCK_WIDTH - 1, BLOCK_HEIGHT - 1))
            Next col
        Next row
    End Sub

    Private Function GetRowColor(row As Integer) As Pixel
        Dim colorNames = {"Red", "Orange", "DarkYellow", "Yellow", "Green", "Blue"}
        Return Colors(colorNames(row Mod 6))
    End Function

    Private Function GetBlockScore(color As Pixel) As Integer
        Select Case color
            Case Colors("Red"), Colors("Orange")
                Return 5
            Case Colors("DarkYellow"), Colors("Yellow")
                Return 3
            Case Colors("Green"), Colors("Blue")
                Return 1
            Case Else
                Return 0
        End Select
    End Function

    Private Sub AddScore(points As Integer)
        If m_gameState = GameState.Playing Then m_score += points
        If m_score >= BONUS_LIFE_EVERY * m_bonusLivesMult Then
            m_lives = Math.Clamp(m_lives + 1, 0, 9)
            m_bonusLivesMult += 1
            sndLifeGained.PlayOnce()
        End If
    End Sub

    Private Sub LoseLife()
        If m_gameState = GameState.Playing Then sndLifeLost.PlayOnce()
        m_lives -= 1
        If m_lives <= 0 Then
            TransferToGameState(GameState.Continue)
        Else
            m_ball.ResetToPaddle(m_paddle)
            ResetPaddleAndBall()
        End If
    End Sub

    Private Function IsLevelCompleted() As Boolean
        Return Aggregate b As Block In m_blocks Into All(Not b.IsActive)
    End Function

    Private Sub NextLevel()
        If m_level >= 9 Then
            TransferToGameState(GameState.GameOver)
        Else
            m_level += 1
            ResetPaddleAndBall()
            CreateLevelBlocks()
        End If
    End Sub

    Private Function IsBallDropped() As Boolean
        Return m_ball.Rect.Y > ScreenHeight
    End Function

    Private Sub ResetPaddleAndBall()
        m_paddle.Rect.X = (m_fieldLeft + m_fieldRight) / 2.0F - (m_paddle.Rect.Width / 2)
        m_paddle.Rect.Y = m_paddleTopY
        m_ball.ResetToPaddle(m_paddle)
    End Sub

    Private Sub ResetGameState()
        m_score = 0
        m_lives = 3
        m_level = 1
        m_ballTimer = 0
        m_continueCountdown = 9
        m_ballMoveInterval = 0.015F
        m_bonusLivesMult = 1
        m_isStarting = False
        TransferToGameState(GameState.Attraction)
    End Sub

    Private Sub ResetGame()
        ResetGameState()
        ResetPaddleAndBall()
        CreateLevelBlocks()
    End Sub

    Private Sub TransferToGameState(gameState As GameState)
        bgmAttraction.Stop()
        bgmContinue.Stop()
        bgmGameOver.Stop()
        bgmMainThemeLoop.Stop()
        Select Case gameState
            Case GameState.Attraction
                bgmAttraction.PlayLooping()
            Case GameState.Playing
                bgmMainThemeLoop.PlayLooping()
            Case GameState.Continue
                bgmContinue.PlayOnce()
            Case GameState.GameOver
                bgmGameOver.PlayOnce()
        End Select
        m_gameState = gameState
    End Sub

    Private Sub UpdateAttractionMode(dt As Single)
        If GetKey(Key.ENTER).Pressed OrElse GetKey(Key.LEFT).Held OrElse
           GetKey(Key.RIGHT).Held Then Exit Sub

        m_ballTimer += dt * Ball.MOVE_SPEED
        If m_ballTimer >= m_ballMoveInterval Then
            m_ballTimer -= m_ballMoveInterval
            m_ball.UpdatePosition()
            CheckCollisions()
            If IsBallDropped() OrElse IsLevelCompleted() Then
                CreateLevelBlocks()
                ResetPaddleAndBall()
            End If
        End If

        Dim targetX = m_ball.Rect.X + (m_ball.Rect.Width / 2) - (m_paddle.Rect.Width / 2)
        If m_paddle.Rect.X < targetX - 2 Then
            m_paddle.Rect.X += m_paddle.MoveSpeed
        ElseIf m_paddle.Rect.X > targetX + 2 Then
            m_paddle.Rect.X -= m_paddle.MoveSpeed
        End If
        m_paddle.ClampToField(m_fieldLeft, m_fieldRight)
    End Sub

    Friend Shared Sub Main()
        With New Program
            If .Construct(320, 210, fullScreen:=True) Then .Start()
        End With
    End Sub
End Class