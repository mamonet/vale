module Emit_common_base

open Ast
open Ast_util
open Parse
open Parse_util
open TypeChecker
open Transform
open Microsoft.FSharp.Math
open System.Numerics

let quick_mods = ref true;
let precise_opaque = ref false;
let reprint_decls_rev = ref ([]:decls)
let no_lemmas = ref false

let require e = Requires (Refined, e)
let ensure e = Ensures (Refined, e)

let get_code_exp (e:exp):exp = map_exp (fun e -> match e with EOp (CodeLemmaOp, [ec; el], _) -> Replace ec | _ -> Unchanged) e
let get_lemma_exp (e:exp):exp = map_exp (fun e -> match e with EOp (CodeLemmaOp, [ec; el], _) -> Replace el | _ -> Unchanged) e

let stateToOp (e:exp):exp map_modify =
  match e with
  | EOp (OperandArg _, [e], _) -> Replace e
  | EOp (StateOp (x, prefix, t), es, _) -> Replace (vaApp_t ("op_" + prefix) es (Some t))
  | _ -> Unchanged

// Turn multiple assignments into series of individual assignments
// Example: x, (ghost var y), z := e;
//  becomes (ghost var tx), (ghost var y), (ghost var tz) := e; x := tx; z := tz;
let eliminate_assign_lhss (s:stmt):stmt list =
  match s with
  | SAssign (lhss, e) ->
      let f (x, dOpt) =
        match dOpt with
        | None ->
            let itmp = string (gen_lemma_sym ()) in
            let xtmp = Reserved ("ltmp" + itmp) in
            let stmp = SAssign ([(x, None)], evar xtmp) in
            ((xtmp, Some (None, Ghost)), [stmp])
        | Some _ -> ((x, dOpt), [])
      let (lhss, ss) = List.unzip (List.map f lhss) in
      (SAssign (lhss, e))::(List.concat ss)
  | _ -> [s]

let varLhsOfId (x:id):lhs = (x, Some (None, NotGhost))

let filter_fun_attr (x, es) =
  match x with
  | Id "recursive" -> !fstar
  | Id ("tactic" | "quick" | "decrease" | "public") -> true
  | _ -> false
  in

let filter_proc_attr (x, es) =
  match x with
  | Id ("timeLimit" | "timeLimitMultiplier" | "tactic" | "quick" | "recursive" | "decrease" | "public") -> true
  | _ -> false
  in

let attr_public (a:attrs) : attrs =
  let isPublic = attrs_get_bool (Id "public") false a in
  if isPublic then [(Id "public", [])] else []

// convert imperative updates to functional let assignments
let rec let_updates_stmts (scope:Map<id, typ option>) (ss:stmt list):(Set<id> * stmt list)=
  let (_, updates, ss_rev) = List.fold let_update_stmt_rev (scope, Set.empty, []) ss in
  let updates = Set.filter (fun x -> Map.containsKey x scope) updates in
  (updates, List.rev ss_rev)
and let_update_stmt_rev (scope:Map<id, typ option>, updates:Set<id>, ss_rev:stmt list) (s:stmt):(Map<id, typ option> * Set<id> * stmt list) =
  let (scope, updates, s) = let_update_stmt scope updates s in
  (scope, updates, s::ss_rev)
and let_update_stmt (scope:Map<id, typ option>) (updates:Set<id>) (s:stmt):(Map<id, typ option> * Set<id> * stmt) =
  let add_unique x t m =
    if Map.containsKey x m then err ("variable '" + (err_id x) + "' already in scope") else
    Map.add x t m
    in
  let find_scope x =
    if Map.containsKey x scope then (x, Map.find x scope)
    else err ("mutable variable '" + (err_id x) + "' not found")
    in
  let make_let updates s =
    let updates = List.map find_scope (Set.toList updates) in
    SLetUpdates (updates, s)
    in
  match s with
  | SLoc (loc, s) ->
      try
        let (scope, updates, s) = let_update_stmt scope updates s in
        (scope, updates, SLoc (loc, s))
      with err -> raise (LocErr (loc, err))
  | SLabel x -> notImplemented "labels"
  | SGoto x -> notImplemented "goto"
  | SReturn -> notImplemented "return"
  | SAssume _ | SAssert _ | SAlias _ | SCalc _ | SForall _ -> (scope, updates, s)
  | SVar (x, t, _, _, _, _) -> (add_unique x t scope, updates, s)
  | SAssign (lhss, e) ->
      let xs_update = List.collect (fun lhs -> match lhs with (x, None) -> [x] | _ -> []) lhss in
      let xs_decls = List.collect (fun lhs -> match lhs with (x, Some (t, _)) -> [(x, t)] | _ -> []) lhss in
      let scope = List.fold (fun scope (x, t) -> add_unique x t scope) scope xs_decls in
      let updates = Set.union (Set.ofList xs_update) updates in
      (scope, updates, s)
  | SLetUpdates _ -> internalErr "SLetUpdates"
  | SBlock b ->
      let (u, b) = let_updates_stmts scope b in
      (scope, Set.union updates u, make_let u (SBlock b))
  | SIfElse (g, e, b1, b2) ->
      let (u1, b1) = let_updates_stmts scope b1 in
      let (u2, b2) = let_updates_stmts scope b2 in
      (scope, Set.unionMany [updates; u1; u2], make_let (Set.union u1 u2) (SIfElse (g, e, b1, b2)))
  | SWhile (e, invs, ed, b) ->
      let (u, b) = let_updates_stmts scope b in
      (scope, Set.union updates u, make_let u (SWhile (e, invs, ed, b)))
  | SExists (xs, ts, e) ->
      let scope = List.fold (fun scope (x, t) -> add_unique x t scope) scope xs in
      (scope, updates, s)

// compute function parameters
// pfIsRet == false ==> pf is input parameter
// pfIsRet == true ==> pf is output return value
let make_fun_param (modifies:bool) (pfIsRet:bool) (pf:pformal):formal list =
  let (x, t, storage, io, attrs) = pf in
  let fx = (x, Some t) in
  match (storage, pfIsRet, modifies) with
  | (XInline, false, false) -> [fx]
  | ((XGhost | XAlias _), _, false) -> []
  | (XOperand, _, false) -> [(x, Some (tOperand (vaOperandTyp t)))]
  | (_, _, true) -> []
  | (XInline, true, _) -> internalErr "XInline"
  | (XState _, _, _) -> internalErr "XState"
  | (XPhysical, _, _) -> internalErr "XPhysical"

let make_fun_params (prets:pformal list) (pargs:pformal list):formal list =
  (List.collect (make_fun_param false true) prets) @
  (List.collect (make_fun_param true true) prets) @
  (List.collect (make_fun_param false false) pargs) @
  (List.collect (make_fun_param true false) pargs)

let fArg (x, t, g, io, a):exp list =
  match g with
  | XInline -> [evar x]
  | XOperand -> [evar x]
//  | XOperand -> [vaApp "op" [EVar x]]
  | _ -> []
  in

let rec hide_ifs (e:exp):exp =
  let thunk (e:exp):exp = ebind Lambda [] [(Id "_", None)] [] e in
  let f (e:exp):exp map_modify =
    match e with
    | EOp (Cond, [e1; e2; e3], t) ->
        let e1 = hide_ifs e1 in
        let e2 = hide_ifs e2 in
        let e3 = hide_ifs e3 in
        Replace (vaApp_t "if" [e1; thunk e2; thunk e3] t)
    | _ -> Unchanged
    in
  map_exp f e

let is_proc (env:env) (x:id) (g:ghost):bool =
  match Map.tryFind x env.procs with Some {pghost = pg} -> pg = g | _ -> false

let specModIo (env:env) (preserveModifies:bool) (loc:loc, s:spec):(inout * (id * typ)) list =
  match s with
  | Requires _ | Ensures _ -> []
  | Modifies (m, e) ->
    (
      let io =
        match m with
        | Modify -> InOut
        | Preserve -> if preserveModifies then InOut else In
        | Read -> In
        in
      match skip_loc (exp_abstract false e) with
      | EVar (x, _) ->
        (
          match Map.tryFind x env.ids with
          | Some (StateInfo (_, _, t, _)) -> [(io, (x, t))]
          | _ -> internalErr ("specMod: could not find variable " + (err_id x))
        )
      | _ -> []
    )
  | SpecRaw _ -> internalErr "SpecRaw"

