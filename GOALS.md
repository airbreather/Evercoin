Goals
=====

Here are my goals for this project, in roughly descending order of importance:

1.  Create a core library whose data model can support a read-only view of the
    main Bitcoin network, without making it too difficult to add support for
    other cryptocurrency networks later (testnet at the minimum).
1.  Create a core library that can be used to populate that data model from a
    live network and present that data in a human-readable fashion.
1.  Expand the above libraries to allow sending new data to the above networks.
1.  Provide the ability to support new cryptocurrency networks with as little
    extra effort as possible on the user's part.
    -   Ideally, networks with the same proof-of-work algorithm should not
        require the user to write new code.
    -   Ideally, the effort required to support a different proof-of-work
        algorithm should be minimal, and not require changing the core (possible
        using MEF).
1.  Provide a graphical user interface for the above.
1.  Comprehensive unit tests for all functionality above.

Those goals are the minimum acceptable for this to be a "success".

Stretch Goals
=============

Other miscellaneous "nice-to-have" goals, in no particular order:

*   Wallets.
*   Integrate multiple configured cryptocurrency networks into one GUI.
*   Support for providing the data needed for mining.
*   Support operating in either full-node or SPV mode, user's choice.
*   Support a reduced-functionality mode that allows a data provider to prune
    redundant data from the blockchain store without sacrificing security.
*   Extensible enough for someone to write their own provider for signing
    transactions.
    -   Usage scenario: I should be able to create a hardware device that only
        exposes "sign these bytes using key 1" and plug that into my main wallet.
*   Extensible enough for someone to write their own blockchain data provider.
    -   Usage scenario: I want to use both Evercoin and the official Bitcoin
        client (not at the same time), but I don't want to store two copies of the
        blockchain.
    -   Usage scenario: I want to store blockchain data on a database instead of
        on the filesystem.
