using GameLogic.Components;

namespace Game.UI
{
    public class MessageUIController : UIController
    {
        public MessageUIController()
        {
            m_UIView = new MessageUIView();
            m_UIModel = new MessageUIModel();
        }
    }
}