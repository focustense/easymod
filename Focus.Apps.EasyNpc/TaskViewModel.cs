using PropertyChanged;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc
{
    [AddINotifyPropertyChangedInterface]
    public class TaskViewModel
    {
        [DependsOn("Status")]
        public bool IsEnded =>
            Status == TaskStatus.Canceled || Status == TaskStatus.Faulted || Status == TaskStatus.RanToCompletion;
        public string Name { get; private init; }
        public TaskStatus Status { get; private set; }

        public TaskViewModel(string name)
        {
            Name = name;
        }

        public void SetTask(Task task)
        {
            Status = task.Status;
            task.ContinueWith(t => Status = t.Status);
        }
    }
}
