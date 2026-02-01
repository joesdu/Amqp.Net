// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Protocol.Types;

/// <summary>
/// AMQP 1.0 descriptor codes for composite types.
/// Format: (domain-id &lt;&lt; 32) | descriptor-id
/// Domain 0x00000000 is reserved for AMQP specification types.
/// </summary>
public static class Descriptor
{
    // ============================================
    // Transport Layer Performatives (Part 2)
    // ============================================

    /// <summary>Open performative (0x00000000:0x00000010)</summary>
    public const ulong Open = 0x0000000000000010;

    /// <summary>Begin performative (0x00000000:0x00000011)</summary>
    public const ulong Begin = 0x0000000000000011;

    /// <summary>Attach performative (0x00000000:0x00000012)</summary>
    public const ulong Attach = 0x0000000000000012;

    /// <summary>Flow performative (0x00000000:0x00000013)</summary>
    public const ulong Flow = 0x0000000000000013;

    /// <summary>Transfer performative (0x00000000:0x00000014)</summary>
    public const ulong Transfer = 0x0000000000000014;

    /// <summary>Disposition performative (0x00000000:0x00000015)</summary>
    public const ulong Disposition = 0x0000000000000015;

    /// <summary>Detach performative (0x00000000:0x00000016)</summary>
    public const ulong Detach = 0x0000000000000016;

    /// <summary>End performative (0x00000000:0x00000017)</summary>
    public const ulong End = 0x0000000000000017;

    /// <summary>Close performative (0x00000000:0x00000018)</summary>
    public const ulong Close = 0x0000000000000018;

    // ============================================
    // Transport Layer Definitions (Part 2)
    // ============================================

    /// <summary>Error composite type (0x00000000:0x0000001d)</summary>
    public const ulong Error = 0x000000000000001d;

    // ============================================
    // Messaging Layer (Part 3)
    // ============================================

    /// <summary>Header section (0x00000000:0x00000070)</summary>
    public const ulong Header = 0x0000000000000070;

    /// <summary>Delivery annotations section (0x00000000:0x00000071)</summary>
    public const ulong DeliveryAnnotations = 0x0000000000000071;

    /// <summary>Message annotations section (0x00000000:0x00000072)</summary>
    public const ulong MessageAnnotations = 0x0000000000000072;

    /// <summary>Properties section (0x00000000:0x00000073)</summary>
    public const ulong Properties = 0x0000000000000073;

    /// <summary>Application properties section (0x00000000:0x00000074)</summary>
    public const ulong ApplicationProperties = 0x0000000000000074;

    /// <summary>Data section (0x00000000:0x00000075)</summary>
    public const ulong Data = 0x0000000000000075;

    /// <summary>AMQP sequence section (0x00000000:0x00000076)</summary>
    public const ulong AmqpSequence = 0x0000000000000076;

    /// <summary>AMQP value section (0x00000000:0x00000077)</summary>
    public const ulong AmqpValue = 0x0000000000000077;

    /// <summary>Footer section (0x00000000:0x00000078)</summary>
    public const ulong Footer = 0x0000000000000078;

    // ============================================
    // Delivery States (Part 3)
    // ============================================

    /// <summary>Received delivery state (0x00000000:0x00000023)</summary>
    public const ulong Received = 0x0000000000000023;

    /// <summary>Accepted delivery state (0x00000000:0x00000024)</summary>
    public const ulong Accepted = 0x0000000000000024;

    /// <summary>Rejected delivery state (0x00000000:0x00000025)</summary>
    public const ulong Rejected = 0x0000000000000025;

    /// <summary>Released delivery state (0x00000000:0x00000026)</summary>
    public const ulong Released = 0x0000000000000026;

    /// <summary>Modified delivery state (0x00000000:0x00000027)</summary>
    public const ulong Modified = 0x0000000000000027;

    // ============================================
    // Sources and Targets (Part 3)
    // ============================================

    /// <summary>Source terminus (0x00000000:0x00000028)</summary>
    public const ulong Source = 0x0000000000000028;

    /// <summary>Target terminus (0x00000000:0x00000029)</summary>
    public const ulong Target = 0x0000000000000029;

    /// <summary>Delete on close lifetime policy (0x00000000:0x0000002b)</summary>
    public const ulong DeleteOnClose = 0x000000000000002b;

    /// <summary>Delete on no links lifetime policy (0x00000000:0x0000002c)</summary>
    public const ulong DeleteOnNoLinks = 0x000000000000002c;

    /// <summary>Delete on no messages lifetime policy (0x00000000:0x0000002d)</summary>
    public const ulong DeleteOnNoMessages = 0x000000000000002d;

    /// <summary>Delete on no links or messages lifetime policy (0x00000000:0x0000002e)</summary>
    public const ulong DeleteOnNoLinksOrMessages = 0x000000000000002e;

    // ============================================
    // Transactions (Part 4)
    // ============================================

    /// <summary>Coordinator target (0x00000000:0x00000030)</summary>
    public const ulong Coordinator = 0x0000000000000030;

    /// <summary>Declare message (0x00000000:0x00000031)</summary>
    public const ulong Declare = 0x0000000000000031;

    /// <summary>Discharge message (0x00000000:0x00000032)</summary>
    public const ulong Discharge = 0x0000000000000032;

    /// <summary>Declared outcome (0x00000000:0x00000033)</summary>
    public const ulong Declared = 0x0000000000000033;

    /// <summary>Transactional state (0x00000000:0x00000034)</summary>
    public const ulong TransactionalState = 0x0000000000000034;

    // ============================================
    // Security (Part 5)
    // ============================================

    /// <summary>SASL mechanisms (0x00000000:0x00000040)</summary>
    public const ulong SaslMechanisms = 0x0000000000000040;

    /// <summary>SASL init (0x00000000:0x00000041)</summary>
    public const ulong SaslInit = 0x0000000000000041;

    /// <summary>SASL challenge (0x00000000:0x00000042)</summary>
    public const ulong SaslChallenge = 0x0000000000000042;

    /// <summary>SASL response (0x00000000:0x00000043)</summary>
    public const ulong SaslResponse = 0x0000000000000043;

    /// <summary>SASL outcome (0x00000000:0x00000044)</summary>
    public const ulong SaslOutcome = 0x0000000000000044;

    /// <summary>
    /// Gets the symbolic name for a descriptor code.
    /// </summary>
    public static string GetName(ulong descriptor)
    {
        return descriptor switch
        {
            Open                  => "amqp:open:list",
            Begin                 => "amqp:begin:list",
            Attach                => "amqp:attach:list",
            Flow                  => "amqp:flow:list",
            Transfer              => "amqp:transfer:list",
            Disposition           => "amqp:disposition:list",
            Detach                => "amqp:detach:list",
            End                   => "amqp:end:list",
            Close                 => "amqp:close:list",
            Error                 => "amqp:error:list",
            Header                => "amqp:header:list",
            DeliveryAnnotations   => "amqp:delivery-annotations:map",
            MessageAnnotations    => "amqp:message-annotations:map",
            Properties            => "amqp:properties:list",
            ApplicationProperties => "amqp:application-properties:map",
            Data                  => "amqp:data:binary",
            AmqpSequence          => "amqp:amqp-sequence:list",
            AmqpValue             => "amqp:amqp-value:*",
            Footer                => "amqp:footer:map",
            Received              => "amqp:received:list",
            Accepted              => "amqp:accepted:list",
            Rejected              => "amqp:rejected:list",
            Released              => "amqp:released:list",
            Modified              => "amqp:modified:list",
            Source                => "amqp:source:list",
            Target                => "amqp:target:list",
            Coordinator           => "amqp:coordinator:list",
            Declare               => "amqp:declare:list",
            Discharge             => "amqp:discharge:list",
            Declared              => "amqp:declared:list",
            TransactionalState    => "amqp:transactional-state:list",
            SaslMechanisms        => "amqp:sasl-mechanisms:list",
            SaslInit              => "amqp:sasl-init:list",
            SaslChallenge         => "amqp:sasl-challenge:list",
            SaslResponse          => "amqp:sasl-response:list",
            SaslOutcome           => "amqp:sasl-outcome:list",
            _                     => $"unknown:0x{descriptor:X16}"
        };
    }

    /// <summary>
    /// Checks if a descriptor is a performative (transport frame body).
    /// </summary>
    public static bool IsPerformative(ulong descriptor) => descriptor is >= Open and <= Close;

    /// <summary>
    /// Checks if a descriptor is a delivery state.
    /// </summary>
    public static bool IsDeliveryState(ulong descriptor) => descriptor is >= Received and <= Modified or TransactionalState or Declared;

    /// <summary>
    /// Checks if a descriptor is a message section.
    /// </summary>
    public static bool IsMessageSection(ulong descriptor) => descriptor is >= Header and <= Footer;

    /// <summary>
    /// Checks if a descriptor is a SASL frame body.
    /// </summary>
    public static bool IsSaslFrame(ulong descriptor) => descriptor is >= SaslMechanisms and <= SaslOutcome;
}
