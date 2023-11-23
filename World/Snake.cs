using System.Collections.Generic;
using System.Drawing;
using Microsoft.Maui.Graphics;

public class Snake
{
    public int ID { get; set; }
    public string Name { get; set; }
    public List<Vector2D> Body { get; private set; } // 使用Vector2D来表示蛇的身体部分
    public Vector2D Direction { get; set; } // 使用Vector2D来表示方向
    public int Score { get; set; }
    public bool IsAlive { get; set; }
    public Color Color { get; set; }

    public Snake()
    {
        Body = new List<Vector2D>();
        Direction = new Vector2D(0, -1); // 默认方向向上
        IsAlive = true;
    }

    public void Move()
    {
        // 如果蛇没有身体部分或不是活着的，则不移动
        if (Body.Count == 0 || !IsAlive)
            return;

        // 计算新的头部位置
        Vector2D head = Body[Body.Count - 1];
        Vector2D newHead = new Vector2D(head.X + Direction.X, head.Y + Direction.Y);

        // 将新头部位置添加到身体的最后
        Body.Add(newHead);

        // 移除蛇尾的第一个部分，使蛇保持相同的长度
        // 除非蛇刚吃到食物，在这种情况下，你可能想跳过这一步
        Body.RemoveAt(0);
    }

    public void ChangeDirection(Vector2D newDirection)
    {
        if (!Direction.IsOppositeCardinalDirection(newDirection))
        {
            Direction = newDirection;
        }
    }

    public void Grow()
    {
        if (Body.Count == 0)
            return;

        // 获取蛇尾部的最后一个部分
        Vector2D tailEnd = Body[0];

        // 计算新尾部的位置
        // 这里假设蛇尾部的最后两个部分是线性的
        Vector2D secondLast = Body.Count > 1 ? Body[1] : tailEnd;
        Vector2D newTailEnd = new Vector2D(tailEnd.X + (tailEnd.X - secondLast.X), tailEnd.Y + (tailEnd.Y - secondLast.Y));

        // 在蛇尾部添加新的部分
        Body.Insert(0, newTailEnd);
    }

    // 可以添加更多依赖于Vector2D的方法，如计算蛇头部和食物之间的角度等
}