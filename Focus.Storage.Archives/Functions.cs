using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Focus.Storage.Archives
{
    public enum ArchiveType
    {
        None = 0,
        TES3,
        TES4,
        FO3,
        SSE,
        FO4,
        FO4DDS,
    }

    delegate bool BsaFileIterationCallback(IntPtr archive, [MarshalAs(UnmanagedType.LPWStr)] string filePath, IntPtr fileRecord, IntPtr folderRecord, IntPtr context);

    enum BsaResultCode : sbyte
    {
        None = 0,
        Exception = -1,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct BsaResultMessageBuffer
    {
        public readonly BsaResultBuffer Buffer;
        public readonly BsaResultMessage Result;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct BsaResultBuffer
    {
        public readonly uint Size;
        public readonly byte[] Data;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct BsaResultMessage
    {
        public readonly BsaResultCode Code;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public readonly string Text;
    }

    static class Functions
    {
        private const string DllPath = "libbsarch.dll";

        [DllImport(DllPath, EntryPoint = "bsa_add_file_from_disk", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaAddFileFromDisk(IntPtr archive, string filePath, string sourcePath);

        [DllImport(DllPath, EntryPoint = "bsa_add_file_from_disk_root", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaAddFileFromDiskRoot(IntPtr archive, string rootDir, string sourcePath);

        [DllImport(DllPath, EntryPoint = "bsa_add_file_from_memory", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaAddFileFromMemory(IntPtr archive, string filePath, uint size, byte[] data);

        [DllImport(DllPath, EntryPoint = "bsa_archive_flags_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaArchiveFlagsGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_archive_flags_set", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern void BsaArchiveFlagsSet(IntPtr archive, uint flags);

        [DllImport(DllPath, EntryPoint = "bsa_archive_type_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern ArchiveType BsaArchiveTypeGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_close", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaClose(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_compress_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool BsaCompressGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_compress_set", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern void BsaCompressSet(IntPtr archive, bool compress);

        [DllImport(DllPath, EntryPoint = "bsa_create", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr BsaCreate();

        [DllImport(DllPath, EntryPoint = "bsa_create_archive", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaCreateArchive(IntPtr archive, string filePath, ArchiveType archiveType, IntPtr entryList);

        [DllImport(DllPath, EntryPoint = "bsa_entry_list_add", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaEntryListAdd(IntPtr entryList, string filePath);

        [DllImport(DllPath, EntryPoint = "bsa_entry_list_count", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaEntryListCount(IntPtr entryList);

        [DllImport(DllPath, EntryPoint = "bsa_entry_list_create", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr BsaEntryListCreate();

        [DllImport(DllPath, EntryPoint = "bsa_entry_list_free", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaEntryListFree(IntPtr entryList);

        [DllImport(DllPath, EntryPoint = "bsa_entry_list_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaEntryListGet(IntPtr entryList, uint index, uint maxLength, StringBuilder buffer);

        [DllImport(DllPath, EntryPoint = "bsa_extract_file", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaExtractFile(IntPtr archive, string filePath, string saveAs);

        [DllImport(DllPath, EntryPoint = "bsa_extract_file_data_by_filename", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessageBuffer BsaExtractFileDataByFilename(IntPtr archive, string filePath);

        [DllImport(DllPath, EntryPoint = "bsa_extract_file_data_by_record", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessageBuffer BsaExtractFileDataByRecord(IntPtr archive, IntPtr fileRecord);

        [DllImport(DllPath, EntryPoint = "bsa_file_count_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaFileCountGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_file_data_free", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaFileDataFree(IntPtr archive, BsaResultBuffer fileDataResult);

        [DllImport(DllPath, EntryPoint = "bsa_file_exists", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool BsaFileExists(IntPtr archive, string filePath);

        [DllImport(DllPath, EntryPoint = "bsa_file_flags_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaFileFlagsGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_file_flags_set", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern void BsaFileFlagsSet(IntPtr archive, uint flags);

        [DllImport(DllPath, EntryPoint = "bsa_filename_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaFilenameGet(IntPtr archive, uint maxLength, StringBuilder buffer);

        [DllImport(DllPath, EntryPoint = "bsa_format_name_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaFormatNameGet(IntPtr archive, uint maxLength, StringBuilder buffer);

        [DllImport(DllPath, EntryPoint = "bsa_free", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaFree(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_get_file_record", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr BsaGetFileRecord(IntPtr archive, uint index);

        [DllImport(DllPath, EntryPoint = "bsa_find_file_record", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr BsaFindFileRecord(IntPtr archive, string filePath);

        [DllImport(DllPath, EntryPoint = "bsa_get_resource_list", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaGetResourceList(IntPtr archive, IntPtr entryList, string folder);

        [DllImport(DllPath, EntryPoint = "bsa_iterate_files", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaIterateFiles(IntPtr archive, BsaFileIterationCallback cb, IntPtr context);

        [DllImport(DllPath, EntryPoint = "bsa_load_from_file", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaLoadFromFile(IntPtr archive, string filePath);

        [DllImport(DllPath, EntryPoint = "bsa_resolve_hash", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaResolveHash(IntPtr archive, ulong hash, IntPtr entryResultList);

        [DllImport(DllPath, EntryPoint = "bsa_save", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern BsaResultMessage BsaSave(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_share_data_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool BsaShareDataGet(IntPtr archive);

        [DllImport(DllPath, EntryPoint = "bsa_share_data_set", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern bool BsaShareDataSet(IntPtr archive, bool shareData);

        [DllImport(DllPath, EntryPoint = "bsa_version_get", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern uint BsaVersionGet(IntPtr archive);
    }
}
