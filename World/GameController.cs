using System.Collections.Generic;
using System.Timers;
using World;

namespace SnakeGame;

public class GameController
{
    private Timer gameTimer; // 定时器用于控制游戏循环
    public List<Snake> Snakes { get; private set; } // 游戏中的所有蛇
    public List<Wall> Walls { get; private set; } // 游戏中的所有墙

    public GameController()
    {
        Snakes = new List<Snake>();
        Walls = new List<Wall>();

        // 初始化游戏定时器
        gameTimer = new Timer(100); // 设置为每100毫秒触发一次
        gameTimer.Elapsed += GameLoop;
        gameTimer.AutoReset = true;
        gameTimer.Enabled = true;
    }

    private void InitializeGameElements()
    {
        // 创建墙壁实例x`
        // 假设我们有一个方法来获取墙壁的位置
        var wallPositions = GetWallPositions();
        foreach (var position in wallPositions)
        {
            Walls.Add(new Wall(position.Start, position.End, position.ID));
        }

        // 创建一条蛇实例
        // 你可能有一个或多个蛇
        Snakes.Add(new Snake());
        // 这里，你可能会根据游戏设计设置初始位置等
    }

    // 游戏主循环，定时器触发
    private void GameLoop(object sender, ElapsedEventArgs e)
    {
        // 更新每条蛇的状态
        foreach (var snake in Snakes)
        {
            snake.Move();
            // 检查蛇是否撞墙或撞到其他蛇等逻辑
            CheckCollisions(snake);
        }

        // 可能还需要更新其他游戏逻辑

        // 通知视图更新
        UpdateView();
    }
    

    // 处理玩家输入
    public void HandleInput(string input, Snake snake)
    {
        Snake snake = Snakes.FirstOrDefault(s => s.ID == snakeId);
        if (snake == null)
            return;

        // 更新蛇的方向
        Vector2D newDirection = input switch
        {
            "Left" => new Vector2D(-1, 0),
            "Right" => new Vector2D(1, 0),
            "Up" => new Vector2D(0, -1),
            "Down" => new Vector2D(0, 1),
            _ => snake.Direction
        };

        snake.ChangeDirection(newDirection);
    }

    // 触发视图更新
    private void UpdateView()
    {
        // 这里将会调用视图的更新方法，例如通知 WorldPanel 重新绘制
        // 你可能会使用事件或直接调用 WorldPanel 的方法
    }

    // 开始新游戏
    public void StartNewGame()
    {
        // 初始化蛇和墙
        Snakes.Clear();
        Walls.Clear();
        InitializeGameElements();
        // 添加蛇和墙的初始化逻辑
    }
}
