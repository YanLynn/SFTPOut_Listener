namespace SFTPOut_Listener
{
    internal sealed class BatchInfo
    {
        public string ProcDate { get; set; }
        public string IssBk { get; set; }
        public string IssBr { get; set; }
        public string Batch { get; set; }
        public string TranType { get; set; }
        public string Sequence { get; set; }

        // spec: 202608057339501601001OWNM<nnnn>.ZIP.PGP
        public string FileName
        {
            get { return ProcDate + IssBk + IssBr + Batch + TranType + Sequence + ".ZIP.PGP"; }
        }

        public string SglFileName { get { return FileName + ".SGL"; } }
    }
}