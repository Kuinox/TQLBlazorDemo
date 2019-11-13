# TQL

A transformer looks like a T-Sql procedure:

```sql
create transformer Name on Target as
begin
-- Transform statements come here…
end
```

The Name and the Target:

- follows the standard Sql Server naming rule: schema.name (with brackets if needed).

- are both optional: a transformer can be a named one or an anonymous one and when a Target is defined it is said to be “scoped”. 

Currently supported transform statements are:

<table>
    <tr>
    	<th>
            Statement
        </th>
        <th>
            Informations
        </th>
    </tr>
    <tr>
    	<td>TransformStatement</td>
        <td><code>Replacer</code>|<code>Injecter</code>|<code>AddParameter</code>|<code>AddColumn</code>|<code>InScope</code></td>
    </tr>
    <tr>
    	<td>AddParameter</td>
        <td><code>add parameter ParameterDecl [(before|after) @parameter]</code></td>
    </tr>
    <tr>
    	<td>ParameterDecl</td>
        <td>Standard T-Sql parameter declaration list.
Options like default value, output keywords, etc. are supported.
Multiple declarations are supported (comma separated list).</td>
    </tr>
    <tr>
    	<td>AddColumn</td>
        <td><code>add column ColumnDecl</code></td>
    </tr>
    <tr>
        <td>ColumnDecl</td>
        <td>Column definition with a column name and a description that uses either = or as: name = definition or definition as name.
Multiple declarations are supported (comma separated list).</td>
    </tr>
    <tr>
    	<td>Replacer</td>
        <td><code>replace LocationFinder with LiteralString</code></td>
    </tr>
    <tr>
        <td>Injecter</td>
        <td>
            <code>inject LiteralString ((and LiteralString around) | before | after) LocationFinder</code></br>
            Or for extension point:</br>
            <code>inject LiteralString into LiteralString</code></br>
            Where the target is an extension point defined with a line comment: </br>
            <code>--&lt;PostCreate/&gt;</code> </br>
            Or if the extension point has already been opened: </br>
            <code>--&lt;PostCreate&gt;</code></br>
            … </br>
            <code>--&lt;/PostCreate&gt;</code></br>
            Note that this extension point may be “reverted”:</br>
            <code>--&lt;PreCreate revert /&gt;</code></br>
            Or</br>
            <code>--&lt;PreDestroy reverse /&gt;</code>
        </td>
    </tr>
    <tr>
        <td>OneLocationFinder</td>
        <td>
            <code>(first [+ n] [out of n]</br> 
            | last [- n] [out of n] </br>
            | single) (CommentStringMatch | NodeSimplePattern)</code></td>
    </tr>
    <tr>
         <td>LocationFinder</td>
         <td>
             <code>OneLocationFinder |</br>
             (all [[out of] n]) (CommentStringMatch | NodeSimplePattern)
             </code>
         </td>
    </tr>
    <tr>
        <td>RangeLocationFinder</td>
        <td><code>
            (after|before) OneLocationFinder</br> 
            | between OneLocationFinder and OneLocationFinder
        </code></td>
    </tr>
    <tr>
        <td>EachFinder</td>
        <td><code>each [[out of] n] (CommentStringMatch | NodeSimplePattern)</code></td>
    </tr>
    <tr>
        <td>CommentStringMatch</td>
        <td>
            A literal string enclosed with, quotes, double quotes or brackets that either:</br>
            - Starts with -- for a line comment:</br>
              "-- prefix" | [-- prefix] | '-- prefix'</br>
              The trimmed prefix must start the left trimmed line comment.
            - Starts and ends with a star comment:</br>
              "/* content */" | [/* content */] | '/* content */'</br>
              The trimmed content must appear in the star comment.
        </td>
    </tr>
    <tr>
        <td>NodeSimplePattern</td>
        <td>
            <code>[part|statement|range] CurlyBracePattern</code></br>
            Defaults to range.</br>
            The default ‘range’ applies to individual tokens whereas ‘part’ or ‘statement’ apply to structural items of the parse tree.
        </td>
    </tr>
    <tr>
        <td>CurlyBracePattern</td>
        <td>
            Curly braces delimited literal string.</br>
            Whitespaces are not relevant, only tokens matter.</br>
            Internal opening or curly braces must be doubled.</br>
            </br>
            Only supported wildcard is ? that replaces any one token.
        </td>
    </tr>
    <tr>
        <td>InScope</td>
        <td><code>
            in RangeLocationFinder | LocationFinder | EachFinder
            begin 
                TransformStatement* 
            end
        </code></td>
    </tr>
</table>

Example:

The following transformer is anonymous and applies to the CK.sGroupCreate object.

Note that his example does not show the Replacer statement (this insane replacement capability can be considered as the nuclear weapon of code transformation :smile:).

```sql
create transformer on CK.sGroupCreate
as
begin
  
  add parameter @ZoneId int = 0 before @GroupIdResult;

  inject "
-- from Zone...
if @ZoneId = 1 throw 50000, 'Zone.SystemZoneHasNoGroup', 1; 
" 
  before single "--[beginsp]";

  in single {insert into CK.tGroup}
  begin
    add column ZoneId = @ZoneId, @ZoneId + @ActorId as ExtraZoneId;
  end

  inject "if @ActorId = 9 throw 50000, 'Security.User9IsMad', 1;" into "PreCreate";
  inject "if @ActorId = 666 throw 50000, 'Security.User666IsEvil', 1;" into "PreCreate";
        
  inject "insert into UX.tLog( Msg ) values( N'Group created.' );" into "PostCreate";
  inject "insert into UX.tOtherLog( Msg ) values( N'Done.' );" into "PostCreate";
end
```

This transformer will transform this:

```sql
create procedure CK.sGroupCreate
(
  @ActorId int,
  @GroupIdResult int output
)
as begin
  if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;

  --[beginsp]

  --<PreCreate />

  exec CK.sActorCreate @ActorId, @GroupIdResult output;
  insert into CK.tGroup( GroupId )
    values( @GroupIdResult );

  --<PostCreate reverse="true" />
      
  --[endsp]
end
```

Into this:

```sql
create procedure CK.sGroupCreate
(
  @ActorId int,
@ZoneId int = 0,
  @GroupIdResult int output
)
as begin
  if @ActorId <= 0 throw 50000, 'Security.AnonymousNotAllowed', 1;

  
-- from Zone...
if @ZoneId = 1 throw 50000, 'Zone.SystemZoneHasNoGroup', 1; 
--[beginsp]

  --<PreCreate >
if @ActorId = 9 throw 50000, 'Security.User9IsMad', 1;
if @ActorId = 666 throw 50000, 'Security.User666IsEvil', 1;
--</PreCreate>

  exec CK.sActorCreate @ActorId, @GroupIdResult output;
  insert into CK.tGroup( GroupId , ZoneId, ExtraZoneId)
    values( @GroupIdResult , @ZoneId, @ZoneId + @ActorId);

  --<PostCreate reverse="true" >
insert into UX.tOtherLog( Msg ) values( N'Done.' );
insert into UX.tLog( Msg ) values( N'Group created.' );
--</PostCreate>
      
  --[endsp]
end 
```

Indentation is, currently, NOT perfect.