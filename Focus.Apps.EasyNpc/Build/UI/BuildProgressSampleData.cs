using Focus.Apps.EasyNpc.Build.Pipeline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build.UI
{
    public class BuildProgressSampleData : IEnumerable<object>
    {
        public IEnumerator<object> GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        private static IEnumerable<object> GetData()
        {
            yield return new
            {
                Name = "First Task",
                ItemName = "Peer out window, chatter at birds",
                MinProgress = 0,
                MaxProgress = 100,
                CurrentProgress = 100,
                IsIndeterminate = false,
                State = BuildTaskState.Completed,
                ErrorMessage = string.Empty,
            };
            yield return new
            {
                Name = "Second Task",
                ItemName = "Curl up and sleep on the freshly laundered towels",
                MinProgress = 0,
                MaxProgress = 100,
                CurrentProgress = 68,
                IsIndeterminate = false,
                State = BuildTaskState.Running,
                ErrorMessage = string.Empty,
            };
            yield return new
            {
                Name = "Third Task",
                ItemName = "Touch water with paw then recoil in horror",
                MinProgress = 0,
                MaxProgress = 80,
                CurrentProgress = 10,
                IsIndeterminate = false,
                State = BuildTaskState.Cancelled,
                ErrorMessage = string.Empty,
            };
            yield return new
            {
                Name = "Fourth Task",
                ItemName = "Ignore the human until she needs to get up, then climb on her lap and sprawl",
                MinProgress = 0,
                MaxProgress = 5,
                CurrentProgress = 2,
                IsIndeterminate = false,
                State = BuildTaskState.Failed,
                ErrorMessage = "Got up too fast",
            };
            yield return new
            {
                Name = "Fifth Task",
                ItemName = "Hack up furballs",
                MinProgress = 0,
                MaxProgress = 4000,
                CurrentProgress = 1289,
                IsIndeterminate = false,
                State = BuildTaskState.Paused,
                ErrorMessage = string.Empty,
            };
            yield return new
            {
                Name = "Sixth Task",
                ItemName = "Meow all night",
                MinProgress = 0,
                MaxProgress = 50,
                CurrentProgress = 0,
                IsIndeterminate = true,
                State = BuildTaskState.NotStarted,
                ErrorMessage = string.Empty,
            };
        }
    }
}
