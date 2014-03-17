Goals
=====

Here are my goals for this project, in roughly descending order of importance:

1.  Create a core library whose data model can support a read-only view of the
    main Bitcoin network, without making it too difficult to add support for
    other cryptocurrency networks later (testnet at the minimum).
1.  Create a core library that can be used to populate that data model from a
    live network and present that data in a human-readable fashion.
1.  Expand the above libraries to allow sending new data to the above networks.
    -   NOTE: This does not need to support signing using a private key.
1.  Provide the ability to support new cryptocurrency networks with as little
    extra effort as possible on the user's part.
    -   Ideally, supporting networks that aren't structurally different from
        Bitcoin should not require the user to write new code.
    -   Supporting a network with a different hashing algorithm should not
        require core changes.  If the algorithm is one that we recognize, it
        shouldn't require any code at all.  For example, Evercoin should fully
        support the Litecoin currency with just a configuration change (the core
        client has built-in support for SCrypt-based hashing using the same
        parameters that Litecoin uses).
1.  Comprehensive unit tests for all functionality above.
1.  Enforce a set of security restrictions on all provided extension points.
    -   All extension providers must be in strong-named assemblies.
    -   The user must explicitly trust the public keys used to sign the
        assemblies that contain extension points, even ones that I may create.
    -   The user must be able to easily revoke their trust of any given public
        key, which should thenceforth disable any extensions coming from
        assemblies for that key until the user explicitly trusts it again.
1.  Provide a basic user interface for the above, probably command-line.

Those goals are the minimum acceptable for this to be a "success".

Stretch Goals
=============

Other miscellaneous "nice-to-have" goals, in no particular order:

*   Installer (MSI / WiX)
*   Wallets.
    -   NOTE: This is high on my priority list.
*   Integrate multiple configured cryptocurrency networks into one interface.
*   Support for providing the data needed for mining.
    -   NOTE: This is high on my priority list.
*   Support operating in either full-node or SPV mode, user's choice.
*   Support a reduced-functionality mode that allows a data provider to prune
    redundant data from the blockchain store without sacrificing security.
*   Extensible enough for someone to write their own provider for signing
    transactions.
    -   Usage scenario: I should be able to create a hardware device that only
        exposes "sign these bytes using key 1" and plug that into my main
        wallet.
    -   Depends on "Wallets".
*   Extensible enough for someone to write their own blockchain data provider as
    a drop-in replacement for core providers (which will probably suck), without
    having to recompile the core code that uses them.
    -   Usage scenario: I want to use both Evercoin and the official Bitcoin
        client (not at the same time), but I don't want to store two copies of
        the blockchain.
    -   Usage scenario: I want to store blockchain data on a database instead of
        on the filesystem.
    -   Usage scenario: The out-of-the-box data providers suck, and it really
        limits how I can use Evercoin.
*   GUI, ideally extensible enough that someone could write compartmentalized
    plugins for creating unusual transactions.
    -   NOTE: This is low on my priority list.  Worst-case scenario, if all the
        other things get completed to my satisfaction, someone who's actually
        decent at this stuff could jump in and make wonderful things happen.
    -   Usage scenario (for extensibility): I have an idea for how to create a
        GUI that supports "contract"-based transactions funded from the user's
        wallet.
