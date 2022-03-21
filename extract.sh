#!/bin/bash
for fsf in {fstar/specs/defs,fstar/specs/hardware,fstar/code/arch,fstar/code/arch/ppc64le}/*.fst; do
    filename=$(basename -- "$fsf")
    fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le $fsf --codegen OCaml --extract_module ${filename%.*}
done
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le obj/fstar/code/arch/ppc64le/PPC64LE.Vale.InsMem.fst --codegen OCaml --extract_module PPC64LE.Vale.InsMem
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le obj/fstar/code/arch/ppc64le/PPC64LE.Vale.InsBasic.fst --codegen OCaml --extract_module PPC64LE.Vale.InsBasic
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le obj/fstar/code/arch/ppc64le/PPC64LE.Vale.InsVector.fst --codegen OCaml --extract_module PPC64LE.Vale.InsVector
cp obj/fstar/code/test/PPC64LE.Test.Memcpy.fst PPC64LE.Test.Memcpy.fst
cp obj/fstar/code/test/PPC64LE.Test.Memcpy.fsti PPC64LE.Test.Memcpy.fsti
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le PPC64LE.Test.Memcpy.fst --codegen OCaml --extract_module PPC64LE.Test.Memcpy
rm PPC64LE.Test.Memcpy.fst
rm PPC64LE.Test.Memcpy.fsti
cp obj/fstar/code/test/PPC64LE.Test.Ins.fst PPC64LE.Test.Ins.fst
cp obj/fstar/code/test/PPC64LE.Test.Ins.fsti PPC64LE.Test.Ins.fsti
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le PPC64LE.Test.Ins.fst --codegen OCaml --extract_module PPC64LE.Test.Ins
rm PPC64LE.Test.Ins.fst
rm PPC64LE.Test.Ins.fsti
cp obj/fstar/code/test/PPC64LE.Test.Vec.fst PPC64LE.Test.Vec.fst
cp obj/fstar/code/test/PPC64LE.Test.Vec.fsti PPC64LE.Test.Vec.fsti
fstar.exe --odir obj --cache_dir obj/cache_checked --include fstar/specs --include fstar/specs/hardware --include fstar/specs/defs --include fstar/code/lib/util --include fstar/code/arch --include fstar/code/arch/ppc64le --include obj/fstar/code/arch/ppc64le PPC64LE.Test.Vec.fst --codegen OCaml --extract_module PPC64LE.Test.Vec
rm PPC64LE.Test.Vec.fst
rm PPC64LE.Test.Vec.fsti
