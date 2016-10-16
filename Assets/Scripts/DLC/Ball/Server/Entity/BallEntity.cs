
public class BallEntity : IQuadTreeItem<BallEntity>
{
    public Rectangle Rect { get; set; }
    public QuadTree<BallEntity> QuadTreeNode { get; set; }
}
