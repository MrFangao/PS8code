public class Wall
{
    public Vector2D Start { get; private set; } // 墙壁的起点
    public Vector2D End { get; private set; } // 墙壁的终点
    public int ID { get; set; } // 墙壁的唯一标识符

    public Wall(Vector2D start, Vector2D end, int id)
    {
        Start = start;
        End = end;
        ID = id;
    }

    // 可以添加更多功能，如检测蛇是否撞到墙壁等
}