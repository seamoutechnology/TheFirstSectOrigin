namespace GameClient.BaseBuilding.AI
{
    public abstract class BaseState
    {
        protected DiscipleAI _ai;

        public BaseState(DiscipleAI ai)
        {
            _ai = ai;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
}
