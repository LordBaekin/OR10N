local mlc = require 'NLua.compiler'.new()

-- Function to parse Lua script and convert to AST
function parse_lua_script(file)
    local ast = mlc:srcfile_to_ast(file)
    return ast
end

-- Traverse the AST to find functions, loops, conditionals, etc.
function traverse_ast(ast)
    local nodes = {}

    local function process_node(node)
        if node.tag == "Function" then
            print("Function found: " .. node[1].name)
            table.insert(nodes, {
                type = "Function",
                name = node[1].name,
                params = node[2], -- Parameters of the function
                body = node[3]    -- Body of the function
            })
        elseif node.tag == "If" then
            print("If statement found")
            table.insert(nodes, { type = "If", body = node })
        -- Add other control structures (For, While, etc.) here
        end
    end

    for _, node in ipairs(ast) do
        process_node(node)
    end

    return nodes
end
