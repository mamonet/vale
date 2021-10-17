let parse_cmdline :
  (string * (unit -> (PPC64LE_Vale_Decls.ins,PPC64LE_Vale_Decls.ocmp) PPC64LE_Machine_s.precode) * int * bool) list -> unit
  =
  fun l  ->
  let printer = PPC64LE_Vale_Decls.gcc in
  (* Extract and print assembly code *)
  PPC64LE_Vale_Decls.print_header printer;
  let _ = List.fold_left (fun label_count (name, code, _, _) ->
                          PPC64LE_Vale_Decls.print_proc name
                                                      (code ())
                                                      label_count printer)
                          (Prims.parse_int "0") l in
  PPC64LE_Vale_Decls.print_footer printer

let _ =
  parse_cmdline [
    ("Copy16", PPC64LE_Test_Memcpy.va_code_Copy16, 0, true);
    ("test_ins", PPC64LE_Test_Ins.va_code_test_ins, 0, true);
    ("test_vec", PPC64LE_Test_Vec.va_code_test_vec, 0, true);
  ]
