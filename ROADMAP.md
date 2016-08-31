Note that this additionally lists features from the closed GUI code.

+ ~~Strike-through~~ indicates features already completed
+ _Emphasis_ indicates further description of the enclosing version or feature

# 0.*
_Basic code required before public release_

## 0.0.*
_Backend capable of creating game databases ready for manipulation and display_

### 0.0.1
+   ~~Working XML reader~~

### 0.0.2
+   Parse core elements of XML game definitions
    -   Create XML schema for definition
    -   _Reference online APIs to prepare for supporting them what fields and
        parameters do they use?), without accessing or parsing them yet_
    -   _Ignore cards for now_
+   Save game definition to database
+   [UWP] Game selection and display of definitions
    -   _For easier testing, Android GUI will be written later_

### 0.0.3
+   Working JSON parser and requests
+   Parse and save translation definitions (remote API -> local names/values)

### 0.0.4
+   Provide method of checking remote definitions
    -   _Scheduling checks to be implemented later due to permission complexity_
+   Define and implement XML update definitions based on translation files
    -   _More fine-grained control over what to change_
    -   _Ensure ability to maintain e.g. row IDs for foreign keys_

### 0.0.5
+   Generate card definitions from `<type><data /></type>` elements
    -   _Validation likely unnecessary due to rarity of encoding cards in XML_
+   Parse cards from JSON APIs according to translation files
+   [UWP] List cards in database

### 0.0.6
+   [UWP] Filter and sort cards
+   [UWP] Select (single) cards to display details
+   [UWP] Generate card data layout from definition files


## 0.1.*
_Collection management_


## 0.2.*
_Android port_


# 1.*
_First public release_



# Future plans
## Pre - 1.*
+   Automatically update local definitions and data from remotes
+   Android GUI