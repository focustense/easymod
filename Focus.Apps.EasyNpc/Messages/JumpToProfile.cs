namespace Focus.Apps.EasyNpc.Messages
{
    public class JumpToProfile
    {
        public FilterOverrides? Filters { get; private init; }

        public JumpToProfile(FilterOverrides? filters)
        {
            Filters = filters;
        }

        public class FilterOverrides
        {
            public bool? Conflicts { get; set; }
            public string? DefaultPlugin { get; set; }
            public bool? FaceChanges { get; set; }
            public string? FacePlugin { get; set; }
            public bool? Missing { get; set; }
            public bool? MultipleChoices { get; set; }
            public bool? NonDlc { get; set; }
            public bool? Wigs { get; set; }
        }
    }
}