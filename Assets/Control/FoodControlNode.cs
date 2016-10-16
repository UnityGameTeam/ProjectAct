
public class FoodControlNode : BallControlNode
{
    protected override void Awake()
    {
        base.Awake();
        RenderNode.RenderType = BallRenderNode.BallRenderType.Food;
    }
}
