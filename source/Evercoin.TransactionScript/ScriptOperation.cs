namespace Evercoin.TransactionScript
{
    /// <summary>
    /// Enumerates the values used as operations in transaction scripts.
    /// </summary>
    /// <remarks>
    /// The values of the enumeration are the actual bytes that will appear on
    /// the wire.  The names of the enumerated values are typically the
    /// canonical names for these constants.
    /// See https://en.bitcoin.it/wiki/Script for more details, but be warned
    /// that details for many less-commonly-used opcodes differ from the actual
    /// implementations in the mainline Bitcoin client.
    /// </remarks>
    public enum ScriptOperation : byte
    {
        /// <summary>
        /// Marks the beginning (inclusive) of a range of opcodes that mean
        /// "push the next N bytes onto the stack as one vector".
        /// </summary>
        BEGIN_OP_DATA = 0x00,

        /// <summary>
        /// Special name for an OP_DATA with N=0.
        /// </summary>
        OP_0 = 0x00,

        /// <summary>
        /// Pushes a false-value (empty vector) onto the stack.
        /// </summary>
        OP_FALSE = 0x00,

        /// <summary>
        /// Marks the end (inclusive) of a range of opcodes that mean
        /// "push the next N bytes onto the stack as one vector".
        /// </summary>
        END_OP_DATA = 0x4b,

        /// <summary>
        /// Interpret the next byte as an 8-bit integer value n.
        /// Then, read the next n bytes, and push that data
        /// onto the stack as one vector.
        /// </summary>
        OP_PUSHDATA1 = 0x4c,

        /// <summary>
        /// Interpret the next two bytes as a 16-bit integer value n.
        /// Then, read the next n bytes, and push that data
        /// onto the stack as one vector.
        /// </summary>
        OP_PUSHDATA2 = 0x4d,

        /// <summary>
        /// Interpret the next four bytes as a 32-bit integer value n.
        /// Then, read the next n bytes, and push that data
        /// onto the stack as one vector.
        /// </summary>
        OP_PUSHDATA4 = 0x4e,

        /// <summary>
        /// Push the value -1 onto the stack.
        /// </summary>
        OP_1NEGATE = 0x4f,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_RESERVED = 0x50,

        /// <summary>
        /// The opcode immediately preceding <see cref="OP_1"/>.
        /// </summary>
        OPCODE_IMMEDIATELY_BEFORE_OP_1 = 0x50,

        /// <summary>
        /// Pushes the value 1 onto the stack.
        /// </summary>
        OP_1 = 0x51,

        /// <summary>
        /// Pushes the value 2 onto the stack.
        /// </summary>
        OP_2 = 0x52,

        /// <summary>
        /// Pushes the value 3 onto the stack.
        /// </summary>
        OP_3 = 0x53,

        /// <summary>
        /// Pushes the value 4 onto the stack.
        /// </summary>
        OP_4 = 0x54,

        /// <summary>
        /// Pushes the value 5 onto the stack.
        /// </summary>
        OP_5 = 0x55,

        /// <summary>
        /// Pushes the value 6 onto the stack.
        /// </summary>
        OP_6 = 0x56,

        /// <summary>
        /// Pushes the value 7 onto the stack.
        /// </summary>
        OP_7 = 0x57,

        /// <summary>
        /// Pushes the value 8 onto the stack.
        /// </summary>
        OP_8 = 0x58,

        /// <summary>
        /// Pushes the value 9 onto the stack.
        /// </summary>
        OP_9 = 0x59,

        /// <summary>
        /// Pushes the value 10 onto the stack.
        /// </summary>
        OP_10 = 0x5a,

        /// <summary>
        /// Pushes the value 11 onto the stack.
        /// </summary>
        OP_11 = 0x5b,

        /// <summary>
        /// Pushes the value 12 onto the stack.
        /// </summary>
        OP_12 = 0x5c,

        /// <summary>
        /// Pushes the value 13 onto the stack.
        /// </summary>
        OP_13 = 0x5d,

        /// <summary>
        /// Pushes the value 14 onto the stack.
        /// </summary>
        OP_14 = 0x5e,

        /// <summary>
        /// Pushes the value 15 onto the stack.
        /// </summary>
        OP_15 = 0x5f,

        /// <summary>
        /// Pushes the value 16 onto the stack.
        /// </summary>
        OP_16 = 0x60,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP = 0x61,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_VER = 0x62,

        /// <summary>
        /// Pops a value off the stack.
        /// If the value is not 0, subsequent statements are executed.
        /// </summary>
        /// <remarks>
        /// Example: OP_6, OP_IF, OP_1, OP_ENDIF
        /// This will leave 1 on the stack.
        /// </remarks>
        OP_IF = 0x63,

        /// <summary>
        /// Pops a value off the stack.
        /// If the value is 0, subsequent statements are executed.
        /// </summary>
        /// <remarks>
        /// Example: OP_0, OP_NOTIF, OP_1, OP_ENDIF
        /// This will leave 1 on the stack.
        /// </remarks>
        OP_NOTIF = 0x64,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_VERIF = 0x65,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_VERNOTIF = 0x66,

        /// <summary>
        /// Iff the preceding <see cref="OP_IF"/>, <see cref="OP_NOTIF"/>, or
        /// <see cref="OP_ELSE"/> check failed, subsequent statements are
        /// executed.
        /// </summary>
        /// <remarks>
        /// Example: OP_0, OP_IF, OP_3, OP_ELSE, OP_1, OP_ENDIF
        /// This will leave 1 on the stack.
        /// </remarks>
        OP_ELSE = 0x67,

        /// <summary>
        /// Ends an if/else block.
        /// </summary>
        OP_ENDIF = 0x68,

        /// <summary>
        /// Pops a value off the stack.
        /// The transaction is invalid if this is not true.
        /// </summary>
        OP_VERIFY = 0x69,

        /// <summary>
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_RETURN = 0x6a,

        /// <summary>
        /// Pops a value off the stack and
        /// pushes that value onto the single alternative stack.
        /// </summary>
        OP_TOALTSTACK = 0x6b,

        /// <summary>
        /// Pops a value off the single alternative stack and
        /// pushes that value onto the stack.
        /// </summary>
        OP_FROMALTSTACK = 0x6c,

        /// <summary>
        /// Pops two values off the stack.
        /// </summary>
        OP_2DROP = 0x6d,

        /// <summary>
        /// Pushes the top two items of the stack onto the stack, reversed.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b" from top to bottom,
        /// then this will push "b", then "a".
        /// The stack will then be "a, b, a, b".
        /// </remarks>
        OP_2DUP = 0x6e,

        /// <summary>
        /// Pushes the top three items of the stack onto the stack, reversed.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b, c" from top to bottom,
        /// then this will push "c", then "b", then "a".
        /// The stack will then be "a, b, c, a, b, c".
        /// </remarks>
        OP_3DUP = 0x6f,

        /// <summary>
        /// Pushes the items three- and four-items deep in the stack onto
        /// the stack, reversed.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b, c, d" from top to bottom,
        /// then this will push "d", then "c".
        /// The stack will then be "c, d, a, b, c, d".
        /// </remarks>
        OP_2OVER = 0x70,

        /// <summary>
        /// Rotates the top six items of the stack by two.
        /// Using valid stack operations, this is equivalent to popping six
        /// items off the stack, then pushing them all back in the order:
        /// fourth, third, second, first, sixth, fifth.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b, c, d, e, f" from top to bottom,
        /// then this will erase and push "f" then "e".
        /// The stack will then be "e, f, a, b, c, d".
        /// </remarks>
        OP_2ROT = 0x71,

        /// <summary>
        /// Pops the top four items off the stack, and pushes
        /// the pairs back on in the same order, but with their items reversed.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b, c, d" from top to bottom,
        /// then this will push "b", then "a", then "d", then "c".
        /// the stack will then be "c, d, a, b".
        /// </remarks>
        OP_2SWAP = 0x72,

        /// <summary>
        /// Pushes the top item of the stack onto the stack, if it is not 0.
        /// </summary>
        OP_IFDUP = 0x73,

        /// <summary>
        /// Pushes the number of items of the stack onto the stack.
        /// </summary>
        OP_DEPTH = 0x74,

        /// <summary>
        /// Pops the top item off the stack.
        /// </summary>
        OP_DROP = 0x75,
        
        /// <summary>
        /// Pushes the top item of the stack onto the stack.
        /// </summary>
        OP_DUP = 0x76,
        
        /// <summary>
        /// Erases the second-from-the-top item from the stack.
        /// Using valid stack operations, this is equivalent to popping two
        /// items off the stack, then pushing back just the first one popped.
        /// </summary>
        OP_NIP = 0x77,

        /// <summary>
        /// Pushes the second-from-the-top item onto the stack.
        /// </summary>
        OP_OVER = 0x78,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value n-deep on the stack.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "4, 3, 2, 1, 0, asdf" from top to bottom,
        /// then the stack will be "asdf, 3, 2, 1, 0, asdf".
        /// </remarks>
        OP_PICK = 0x79,

        /// <summary>
        /// Same as <see cref="OP_PICK"/>, except erases the value as well.
        /// Using valid stack operations, this is equivalent to popping an item
        /// off the stack, interpreting the result as an integer value, then
        /// popping that many items off the stack (saving the values as "old"),
        /// then popping the next item off the stack (saving the value as
        /// "found"), then pushing the values from "old" in the reverse of the
        /// order they were returned, then pushing "found" onto the stack.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "4, 3, 2, 1, 0, asdf" from top to bottom,
        /// then the stack will be "asdf, 3, 2, 1, 0".
        /// </remarks>
        OP_ROLL = 0x7a,

        /// <summary>
        /// Rotates the top three items of the stack by one.
        /// Using valid stack operations, this is equivalent to popping three
        /// items off the stack, then pushing them all back in the order:
        /// second, first, third.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b, c" from top to bottom,
        /// then this will erase and push "c".
        /// The stack will then be "c, a, b".
        /// </remarks>
        OP_ROT = 0x7b,

        /// <summary>
        /// Swaps the top two items on the stack.
        /// Using valid stack operations, this is equivalent to popping two
        /// items off the stack, then pushing them back in the same order.
        /// </summary>
        OP_SWAP = 0x7c,

        /// <summary>
        /// Copies the item at the top of the stack two items deep.
        /// Using valid stack operations, this is equivalent to popping two
        /// items off the stack, then pushing them back in the order:
        /// first, second, first.
        /// </summary>
        /// <remarks>
        /// Example: if the stack is "a, b" from top to bottom,
        /// then the stack will be "a, b, a".
        /// </remarks>
        OP_TUCK = 0x7d,

        /// <summary>
        /// Concatenates two strings at the top of the stack.
        /// This is currently disabled.
        /// </summary>
        OP_CAT = 0x7e,

        /// <summary>
        /// Returns a substring of the string at the top of the stack.
        /// This is currently disabled.
        /// </summary>
        OP_SUBSTR = 0x7f,

        /// <summary>
        /// Keeps only the leftmost n characters of the string at the top of
        /// the stack.
        /// This is currently disabled.
        /// </summary>
        OP_LEFT = 0x80,

        /// <summary>
        /// Keeps only the rightmost n characters of the string at the top of
        /// the stack.
        /// This is currently disabled.
        /// </summary>
        OP_RIGHT = 0x81,

        /// <summary>
        /// Pushes the size of the item at the top of the stack.
        /// </summary>
        OP_SIZE = 0x82,

        /// <summary>
        /// Flips all the bits of the input.
        /// This is currently disabled.
        /// </summary>
        OP_INVERT = 0x83,

        /// <summary>
        /// Boolean AND of each bit of the inputs.
        /// This is currently disabled.
        /// </summary>
        OP_AND = 0x84,

        /// <summary>
        /// Boolean OR of each bit of the inputs.
        /// This is currently disabled.
        /// </summary>
        OP_OR = 0x85,

        /// <summary>
        /// Boolean XOR of each bit of the inputs.
        /// This is currently disabled.
        /// </summary>
        OP_XOR = 0x86,
        
        /// <summary>
        /// Pops the top two items off the stack and pushes
        /// 1 if they are exactly equal, 0 otherwise.
        /// </summary>
        OP_EQUAL = 0x87,

        /// <summary>
        /// Shortcut for <see cref="OP_EQUAL"/>
        /// followed by <see cref="OP_VERIFY"/>.
        /// </summary>
        OP_EQUALVERIFY = 0x88,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_RESERVED1 = 0x89,

        /// <summary>
        /// This value is reserved.
        /// The transaction is invalid if this is executed.
        /// </summary>
        OP_RESERVED2 = 0x8a,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value n+1 onto the stack.
        /// </summary>
        OP_1ADD = 0x8b,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value n-1 onto the stack.
        /// </summary>
        OP_1SUB = 0x8c,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value n*2 onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_2MUL = 0x8d,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value n/2 onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_2DIV = 0x8e,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the value -n onto the stack.
        /// </summary>
        OP_NEGATE = 0x8f,

        /// <summary>
        /// Pops the item at the top of the stack and interprets it as an
        /// integer value n, then pushes the abs(n) onto the stack.
        /// </summary>
        OP_ABS = 0x90,

        /// <summary>
        /// Pops the item at the top of the stack and pushes
        /// 1 if it is equal to 0, 0 otherwise.
        /// </summary>
        OP_NOT = 0x91,

        /// <summary>
        /// Pops the item at the top of the stack and pushes
        /// 0 if it is equal to 0, 1 otherwise.
        /// </summary>
        OP_0NOTEQUAL = 0x92,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x + y onto the stack.
        /// </summary>
        OP_ADD = 0x93,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x - y onto the stack.
        /// </summary>
        OP_SUB = 0x94,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x * y onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_MUL = 0x95,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x / y onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_DIV = 0x96,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x % y onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_MOD = 0x97,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x &lt;&lt; y onto the stack.
        /// This is currently disabled.
        /// </summary>
        OP_LSHIFT = 0x98,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x >> y onto the stack.
        /// The shift is arithmetic (sign-preserving).
        /// This is currently disabled.
        /// </summary>
        OP_RSHIFT = 0x99,

        /// <summary>
        /// Pops two items off the top of the stack and pushes
        /// 1 onto the stack if both are non-zero, 0 otherwise.
        /// </summary>
        OP_BOOLAND = 0x9a,

        /// <summary>
        /// Pops two items off the top of the stack and pushes
        /// 0 onto the stack if both are zero, 1 otherwise.
        /// </summary>
        OP_BOOLOR = 0x9b,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x == y onto the stack.
        /// </summary>
        OP_NUMEQUAL = 0x9c,

        /// <summary>
        /// Shortcut for <see cref="OP_NUMEQUAL"/>
        /// followed by <see cref="OP_VERIFY"/>.
        /// </summary>
        OP_NUMEQUALVERIFY = 0x9d,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x != y onto the stack.
        /// </summary>
        OP_NUMNOTEQUAL = 0x9e,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x &lt; y onto the stack.
        /// </summary>
        OP_LESSTHAN = 0x9f,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x &gt; y onto the stack.
        /// </summary>
        OP_GREATERTHAN = 0xa0,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x &lt;= y onto the stack.
        /// </summary>
        OP_LESSTHANOREQUAL = 0xa1,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes x &gt;= y onto the stack.
        /// </summary>
        OP_GREATERTHANOREQUAL = 0xa2,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes min(x, y) onto the stack.
        /// </summary>
        OP_MIN = 0xa3,

        /// <summary>
        /// Pops two items off the top of the stack and interprets them as
        /// integer values x and y, then pushes max(x, y) onto the stack.
        /// </summary>
        OP_MAX = 0xa4,

        /// <summary>
        /// Pops three items off the top of the stack and interprets them as
        /// integer values x, min, max, then pushes 1 onto the stack if
        /// (x &gt;= min) && (x &lt; max).
        /// </summary>
        OP_WITHIN = 0xa5,

        /// <summary>
        /// Pops an item off the top of the stack, and pushes
        /// ripemd160(item) onto the stack.
        /// </summary>
        OP_RIPEMD160 = 0xa6,

        /// <summary>
        /// Pops an item off the top of the stack, and pushes
        /// sha1(item) onto the stack.
        /// </summary>
        OP_SHA1 = 0xa7,

        /// <summary>
        /// Pops an item off the top of the stack, and pushes
        /// sha256(item) onto the stack.
        /// </summary>
        OP_SHA256 = 0xa8,

        /// <summary>
        /// Pops an item off the top of the stack, and pushes
        /// ripemd160(sha256(item)) onto the stack.
        /// </summary>
        OP_HASH160 = 0xa9,

        /// <summary>
        /// Pops an item off the top of the stack, and pushes
        /// sha256(sha256(item)) onto the stack.
        /// </summary>
        OP_HASH256 = 0xaa,

        /// <summary>
        /// Does nothing.
        /// Used as a marker for signature verification.
        /// </summary>
        OP_CODESEPARATOR = 0xab,

        /// <summary>
        /// Pops two items off the top of the stack, and interprets them as a
        /// public key and signature, respectively.  Then, hashes the entire
        /// transaction's outputs, inputs, and script (from the most recently-
        /// executed <see cref="OP_CODESEPARATOR"/> to the end).  If the
        /// signature is valid for the hash and public key, then pushes 1 onto
        /// the stack.  0 otherwise.
        /// </summary>
        OP_CHECKSIG = 0xac,

        /// <summary>
        /// Shortcut for <see cref="OP_CHECKSIG"/>
        /// followed by <see cref="OP_VERIFY"/>.
        /// </summary>
        OP_CHECKSIGVERIFY = 0xad,

        /// <summary>
        /// A version of <see cref="OP_CHECKSIG"/> that allows multiple public
        /// keys and multiple signatures to sign, all of which will be checked
        /// at once.
        /// TODO: actually document this... tired right now.
        /// </summary>
        OP_CHECKMULTISIG = 0xae,

        /// <summary>
        /// Shortcut for <see cref="OP_CHECKMULTISIG"/>
        /// followed by <see cref="OP_VERIFY"/>.
        /// </summary>
        OP_CHECKMULTISIGVERIFY = 0xaf,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP1 = 0xb0,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP2 = 0xb1,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP3 = 0xb2,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP4 = 0xb3,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP5 = 0xb4,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP6 = 0xb5,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP7 = 0xb6,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP8 = 0xb7,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP9 = 0xb8,

        /// <summary>
        /// Does nothing.
        /// </summary>
        OP_NOP10 = 0xb9,

        /// <summary>
        /// Marks the beginning (inclusive) of a range of opcodes that are
        /// unused.
        /// </summary>
        BEGIN_UNUSED = 0xba,

        /// <summary>
        /// Marks the end (inclusive) of a range of opcodes that are unused.
        /// </summary>
        END_UNUSED = 0xff
    }
}
