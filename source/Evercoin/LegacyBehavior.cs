namespace Evercoin
{
    /// <summary>
    /// Legacy behaviors are unintuitive quirks present in released
    /// versions of the reference implementations that need to be
    /// emulated in order for any client to be compatible.
    /// </summary>
    public enum LegacyBehavior
    {
        /// <summary>
        /// OP_CHECKMULTISIG pops an extra value off the stack when executed.
        /// </summary>
        /// <remarks>
        /// https://en.bitcoin.it/wiki/Hardfork_Wishlist#Bug_fixes
        /// http://sourceforge.net/p/bitcoin/mailman/message/28004076/
        /// </remarks>
        CheckMultisigPopsOneExtraValueOffTheStack,

        /// <summary>
        /// Whenever the difficulty retargets, the timestamp of the last block
        /// in the retarget window is ignored, thus the difficulty could
        /// theoretically be manipulated by the miner of that last block.
        /// </summary>
        /// <remarks>
        /// https://litecoin.info/Time_warp_attack
        /// https://bitcointalk.org/index.php?topic=43692.msg521772#msg521772
        /// </remarks>
        DifficultyRetargetingIgnoresTheLastOneBlockInTheWindow,

        /// <summary>
        /// No output from the genesis block's coinbase transaction may appear
        /// in the inputs of any other transaction.
        /// </summary>
        /// <remarks>
        /// https://bitcointalk.org/index.php?topic=119530.msg1286692#msg1286692
        /// http://bitcoin.stackexchange.com/a/10019/14109
        /// </remarks>
        GenesisBlockCoinbaseOutputsCannotBeSpent
    }
}
