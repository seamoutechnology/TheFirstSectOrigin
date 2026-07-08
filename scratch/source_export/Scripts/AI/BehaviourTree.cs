using System.Collections.Generic;
using UnityEngine;

namespace TFSO.AI
{
    public enum NodeStatus { Success, Failure, Running }

    public abstract class Node
    {
        public abstract NodeStatus Execute();
    }

    public class Sequence : Node
    {
        private List<Node> _children = new List<Node>();
        public Sequence(List<Node> children) => _children = children;

        public override NodeStatus Execute()
        {
            foreach (var node in _children)
            {
                var status = node.Execute();
                if (status != NodeStatus.Success) return status;
            }
            return NodeStatus.Success;
        }
    }

    public class Selector : Node
    {
        private List<Node> _children = new List<Node>();
        public Selector(List<Node> children) => _children = children;

        public override NodeStatus Execute()
        {
            foreach (var node in _children)
            {
                var status = node.Execute();
                if (status != NodeStatus.Failure) return status;
            }
            return NodeStatus.Failure;
        }
    }

    public class DiscipleActionNode : Node
    {
        private System.Action _action;
        public DiscipleActionNode(System.Action action) => _action = action;

        public override NodeStatus Execute()
        {
            _action?.Invoke();
            return NodeStatus.Success;
        }
    }
}
