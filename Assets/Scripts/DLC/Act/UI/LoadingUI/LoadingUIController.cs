using GameLogic.Components;

namespace Game.UI
{
    public class LoadingUIController : UIController
    {
        public LoadingUIController()
        {
            m_UIView = new LoadingUIView();
            m_UIModel = new LoadingUIModel();
        }
    }
}

