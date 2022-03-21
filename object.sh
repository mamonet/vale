#!/bin/bash
max_loop=0
for module in obj/*.ml; do
    max_loop=$[$max_loop +1]
done
modules=()
modules_count=0
for ((count = 0; count < max_loop; count++)); do
  for module in obj/*.ml; do
    filename=$(basename -- "$module")
    exist=0
    for mod in "${modules[@]}"; do
      if [ $mod = "$filename" ]; then
        exist=1
        break
      fi
    done
    if [ $exist -eq 1 ]; then
      continue
    fi
    OCAMLPATH="$FSTAR_HOME/bin" ocamlfind opt -package fstarlib -linkpkg -I obj -w -8-20-26 -c obj/${filename%.*}.ml -o obj/${filename%.*}.cmx
    if [ $? -eq 0 ]; then
      modules+=("$filename")
      modules_count=$[$modules_count +1]
    fi
  done
done
echo "Modules" $modules_count
