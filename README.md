# AeoB Utilities

Have you ever wondered what these files were?

![image](https://user-images.githubusercontent.com/3755345/198696673-6e03b711-4e8f-4867-8cb2-12b66e504b6c.png)

(non exahustive list)

Have you ever needed to modify them? If yes, then this tool is for you.

AeoB Utilities is a tool designed to parse, convert, make readable, and rebuild AeoB (ACPI EVALUATE OUTPUT BUFFER) binary files commonly found shipping on Qualcomm Technologies Inc. drivers.

These files are akin to the structure used in Windows for ACPI_EVALUATE_OUTPUT_BUFFER_V1 (hence the name AeoB), but are post processed already from ACPI's aml bytecode making it troublesome for people to inspect and edit with ease.

The tool also provides support for Adreno specific file formats, and can be used as well to debug these buffers if copied from WinDBG memory dump.

## Examples

SMMC.bin (SMMU Driver resource binary) converted output:

```asl
Package (0x00E7)
{
    "DEVICE",
    "\_SB.MMU0",
    Package (0x00CA)
    {
        "COMPONENT",
        0x0000000000000000,
        Package (0x0054)
        {
            "FSTATE",
            0x0000000000000000,
            Package (0x0039)
            {
                "CLOCK",
                Package (0x002B)
                {
                    "gcc_hlos1_vote_mmu_tcu_clk",
                    0x0000000000000001,
                },
            },
        },
        Package (0x0054)
        {
            "FSTATE",
            0x0000000000000001,
            Package (0x0039)
            {
                "CLOCK",
                Package (0x002B)
                {
                    "gcc_hlos1_vote_mmu_tcu_clk",
                    0x0000000000000002,
                },
            },
        },
    },
},
Package (0x016F)
{
    "DEVICE",
    "\_SB.MMU1",
    Package (0x0152)
    {
        "COMPONENT",
        0x0000000000000000,
        Package (0x0098)
        {
            "FSTATE",
            0x0000000000000000,
            Package (0x003F)
            {
                "FOOTSWITCH",
                Package (0x002C)
                {
                    "gcc_hlos1_vote_gpu_smmu_gds",
                    0x0000000000000001,
                },
            },
            Package (0x003A)
            {
                "CLOCK",
                Package (0x002C)
                {
                    "gcc_hlos1_vote_gpu_smmu_clk",
                    0x0000000000000001,
                },
            },
        },
        Package (0x0098)
        {
            "FSTATE",
            0x0000000000000001,
            Package (0x003A)
            {
                "CLOCK",
                Package (0x002C)
                {
                    "gcc_hlos1_vote_gpu_smmu_clk",
                    0x0000000000000002,
                },
            },
            Package (0x003F)
            {
                "FOOTSWITCH",
                Package (0x002C)
                {
                    "gcc_hlos1_vote_gpu_smmu_gds",
                    0x0000000000000002,
                },
            },
        },
    },
},
```
