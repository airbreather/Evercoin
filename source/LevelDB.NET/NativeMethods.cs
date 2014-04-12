using System;
using System.Runtime.InteropServices;

namespace LevelDb
{
    internal delegate void PutAction(IntPtr state, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key, ulong keyLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] value, ulong valueLength);
    internal delegate void DeleteAction(IntPtr state, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key, ulong keyLength);
    internal delegate void DestructorAction(IntPtr state);
    internal delegate int ComparatorAction(IntPtr state, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] firstKey, ulong firstKeyLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] secondKey, ulong secondKeyLength);
    [return: MarshalAs(UnmanagedType.LPStr)] internal delegate string GetNameAction(IntPtr state);
    [return: MarshalAs(UnmanagedType.LPArray)] internal delegate byte[] CreateFilterAction(IntPtr state, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[][] keys, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysUInt, SizeParamIndex = 3)] ulong[] keyLengths, int numberOfKeys, out ulong filterLength);
    [return: MarshalAs(UnmanagedType.U1)] internal delegate bool CheckFilterAction(IntPtr state, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key, ulong keyLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] filter, ulong filterLength);

    /// <summary>
    /// Native method P/Invoke declarations for LevelDB
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_open(IntPtr options,
                                                 [MarshalAs(UnmanagedType.LPStr)] string name,
                                                 [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern void leveldb_close(IntPtr db);

        [DllImport("leveldb-native")]
        public static extern void leveldb_put(IntPtr db,
                                              IntPtr writeOptions,
                                              [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] key,
                                              ulong keyLength,
                                              [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] value,
                                              ulong valueLength,
                                              [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern void leveldb_delete(IntPtr db,
                                                 IntPtr writeOptions,
                                                 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] key,
                                                 ulong keyLength,
                                                 [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern void leveldb_write(IntPtr db,
                                                IntPtr writeOptions,
                                                IntPtr writeBatch,
                                                [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_get(IntPtr db,
                                                IntPtr readOptions,
                                                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] key,
                                                ulong keyLength,
                                                out ulong valueLength,
                                                [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_create_iterator(IntPtr db,
                                                            IntPtr readOptions);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_create_snapshot(IntPtr db);

        [DllImport("leveldb-native")]
        public static extern void leveldb_release_snapshot(IntPtr db,
                                                           IntPtr snapshot);

        [DllImport("leveldb-native")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string leveldb_property_value(IntPtr db,
                                                           [MarshalAs(UnmanagedType.LPStr)] string propertyName);

        [DllImport("leveldb-native")]
        public static extern void leveldb_approximate_sizes(IntPtr db,
                                                            int num_ranges,
                                                            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] rangeStartKey,
                                                            ulong rangeStartKeyLength,
                                                            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] rangeLimitKey,
                                                            ulong rangeLimitKeyLength,
                                                            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysUInt)] ulong[] sizes);

        [DllImport("leveldb-native")]
        public static extern void leveldb_compact_range(IntPtr db,
                                                        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] startKey,
                                                        ulong startKeyLength,
                                                        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] limitKey,
                                                        ulong limitKeyLength);

        [DllImport("leveldb-native")]
        public static extern void leveldb_destroy_db(IntPtr options,
                                                     [MarshalAs(UnmanagedType.LPStr)] string name,
                                                     [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern void leveldb_repair_db(IntPtr options,
                                                    [MarshalAs(UnmanagedType.LPStr)] string name,
                                                    [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_destroy(IntPtr iter);

        [DllImport("leveldb-native")]
        public static extern bool leveldb_iter_valid(IntPtr iter);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_seek_to_first(IntPtr iter);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_seek_to_last(IntPtr iter);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_seek(IntPtr iter,
                                                    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] key,
                                                    ulong keyLength);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_next(IntPtr iter);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_prev(IntPtr iter);

        [DllImport("leveldb-native")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string leveldb_iter_key(IntPtr iter,
                                                     out ulong keyLength);

        [DllImport("leveldb-native")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string leveldb_iter_value(IntPtr iter,
                                                       out ulong valueLength);

        [DllImport("leveldb-native")]
        public static extern void leveldb_iter_get_error(IntPtr iter,
                                                         [MarshalAs(UnmanagedType.LPStr)] out string error);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_writebatch_create();

        [DllImport("leveldb-native")]
        public static extern void leveldb_writebatch_destroy(IntPtr writeBatch);

        [DllImport("leveldb-native")]
        public static extern void leveldb_writebatch_clear(IntPtr writeBatch);

        [DllImport("leveldb-native")]
        public static extern void leveldb_writebatch_put(IntPtr writeBatch,
                                                         [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key,
                                                         ulong keyLength,
                                                         [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] value,
                                                         ulong valueLength);

        [DllImport("leveldb-native")]
        public static extern void leveldb_writebatch_delete(IntPtr writeBatch,
                                                            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] key,
                                                            ulong keyLength);

        [DllImport("leveldb-native")]
        public static extern void leveldb_writebatch_iterate(IntPtr writeBatch,
                                                             IntPtr handle,
                                                             [MarshalAs(UnmanagedType.FunctionPtr)] PutAction putCallback,
                                                             [MarshalAs(UnmanagedType.FunctionPtr)] DeleteAction deleteCallback);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_options_create();

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_destroy(IntPtr options);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_comparator(IntPtr options,
                                                                 IntPtr comparator);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_filter_policy(IntPtr options,
                                                                    IntPtr filterPolicy);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_create_if_missing(IntPtr options,
                                                                        [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_error_if_exists(IntPtr options,
                                                                      [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_paranoid_checks(IntPtr options,
                                                                      [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_env(IntPtr options,
                                                          IntPtr env);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_info_log(IntPtr options,
                                                               IntPtr logger);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_write_buffer_size(IntPtr options,
                                                                        ulong size);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_max_open_files(IntPtr options,
                                                                     int value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_cache(IntPtr options,
                                                            IntPtr cache);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_block_size(IntPtr options,
                                                                 ulong size);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_block_restart_interval(IntPtr options,
                                                                             int interval);

        [DllImport("leveldb-native")]
        public static extern void leveldb_options_set_compression(IntPtr options,
                                                                  CompressionOption value);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_comparator_create(IntPtr handle,
                                                              [MarshalAs(UnmanagedType.FunctionPtr)] DestructorAction destructor,
                                                              [MarshalAs(UnmanagedType.FunctionPtr)] ComparatorAction comparator,
                                                              [MarshalAs(UnmanagedType.FunctionPtr)] GetNameAction nameGetter);

        [DllImport("leveldb-native")]
        public static extern void leveldb_comparator_destroy(IntPtr comparator);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_filterpolicy_create(IntPtr handle,
                                                                [MarshalAs(UnmanagedType.FunctionPtr)] DestructorAction destructor,
                                                                [MarshalAs(UnmanagedType.FunctionPtr)] GetNameAction nameGetter,
                                                                [MarshalAs(UnmanagedType.FunctionPtr)] CreateFilterAction createAction,
                                                                [MarshalAs(UnmanagedType.FunctionPtr)] CheckFilterAction checkAction);

        [DllImport("leveldb-native")]
        public static extern void leveldb_filterpolicy_destroy(IntPtr filterPolicy);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_filterpolicy_create_bloom(int bitsPerKey);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_readoptions_create();

        [DllImport("leveldb-native")]
        public static extern void leveldb_readoptions_destroy(IntPtr readOptions);

        [DllImport("leveldb-native")]
        public static extern void leveldb_readoptions_set_verify_checksums(IntPtr readOptions,
                                                                           [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_readoptions_set_fill_cache(IntPtr readOptions,
                                                                     [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern void leveldb_readoptions_set_snapshot(IntPtr readOptions,
                                                                   IntPtr snapshot);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_writeoptions_create();

        [DllImport("leveldb-native")]
        public static extern void leveldb_writeoptions_destroy(IntPtr writeOptions);

        [DllImport("leveldb-native")]
        public static extern void leveldb_writeoptions_set_sync(IntPtr writeOptions,
                                                                [MarshalAs(UnmanagedType.U1)] bool value);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_cache_create_lru(ulong capacity);

        [DllImport("leveldb-native")]
        public static extern void leveldb_cache_destroy(IntPtr cache);

        [DllImport("leveldb-native")]
        public static extern IntPtr leveldb_create_default_env();

        [DllImport("leveldb-native")]
        public static extern void leveldb_env_destroy(IntPtr env);

        [DllImport("leveldb-native")]
        public static extern void leveldb_free(IntPtr ptr);

        [DllImport("leveldb-native")]
        public static extern int leveldb_major_version();

        [DllImport("leveldb-native")]
        public static extern int leveldb_minor_version();
    }
}
