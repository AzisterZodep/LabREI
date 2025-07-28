from flask import Flask, request, jsonify, render_template
from flask_cors import CORS
import threading
import json
import datetime
from werkzeug.security import generate_password_hash, check_password_hash
import os # Import os module to check for file existence

app = Flask(__name__)
CORS(app)

# Define file paths for persistence
PECAS_FILE = 'pecas.json'
COMPUTADORES_FILE = 'computadores.json'
HISTORICO_FILE = 'historico.json'
USERS_FILE = 'users.json'
AUDIT_LOG_FILE = 'audit_log.json'

pecas_db = []
computadores_db = []
historico_pcs_excluidos_db = []
usuarios_db = []
audit_log_db = []

next_peca_id = 1
next_pc_id = 1
next_user_id = 1

# --- Persistence Functions ---
def load_data():
    global pecas_db, computadores_db, historico_pcs_excluidos_db, next_peca_id, next_pc_id
    global usuarios_db, audit_log_db, next_user_id
    # Load Pecas
    if os.path.exists(PECAS_FILE):
        with open(PECAS_FILE, 'r') as f:
            pecas_db = json.load(f)
            if pecas_db: # If there are existing pieces, set the next ID appropriately
                next_peca_id = max(p['id'] for p in pecas_db) + 1
    
    # Load Computadores
    if os.path.exists(COMPUTADORES_FILE):
        with open(COMPUTADORES_FILE, 'r') as f:
            computadores_db = json.load(f)
            if computadores_db: # If there are existing computers, set the next ID appropriately
                next_pc_id = max(c['id'] for c in computadores_db) + 1

    # Load Historico
    if os.path.exists(HISTORICO_FILE):
        with open(HISTORICO_FILE, 'r') as f:
            historico_pcs_excluidos_db = json.load(f)

    if os.path.exists(USERS_FILE):
        with open(USERS_FILE, 'r') as f:
            usuarios_db = json.load(f)
            if usuarios_db:
                next_user_id = max(u['id'] for u in usuarios_db) + 1

    audit_log_db.clear() # Limpa a lista antes de carregar
    if os.path.exists(AUDIT_LOG_FILE):
        with open(AUDIT_LOG_FILE, 'r') as f:
            for line in f:
                line = line.strip()
                if line:
                    try:
                        audit_log_db.append(json.loads(line))
                    except json.JSONDecodeError:
                        print(f"Aviso: Linha inválida no {AUDIT_LOG_FILE}: {line}")
    else:
        with open(AUDIT_LOG_FILE, 'w') as f: 
            pass

    print("Dados carregados com sucesso!")

def save_data():
    with open(PECAS_FILE, 'w') as f:
        json.dump(pecas_db, f, indent=4)
    with open(COMPUTADORES_FILE, 'w') as f:
        json.dump(computadores_db, f, indent=4)
    with open(HISTORICO_FILE, 'w') as f:
        json.dump(historico_pcs_excluidos_db, f, indent=4)
    if os.path.exists(USERS_FILE):
        with open(USERS_FILE, 'w') as f:
            json.dump(usuarios_db, f, indent=4)
    print("Dados salvos com sucesso!")

def log_action(user_id, username, action_type, target_id=None, details=None):
    log_entry = {
        "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "user_id": user_id,
        "username": username,
        "action": action_type,
        "target_id": target_id,
        "details": details if details is not None else {}
    }
    
    # Adiciona a entrada ao log em memória (para get_audit_log)
    audit_log_db.append(log_entry)

    # --- Salva a entrada diretamente no ficheiro de log de auditoria (apenas adicionar) ---
    with open(AUDIT_LOG_FILE, 'a') as f: # Usa o modo 'a' para adicionar
        json.dump(log_entry, f) # Escreve o objeto JSON
        f.write('\n') # Adiciona uma nova linha para o formato JSON Lines

def get_current_user_info():
    # This is a placeholder. In a real app, you'd get this from a session or JWT.
    # For now, let's assume 'X-User-ID' and 'X-Username' headers are provided.
    user_id = request.headers.get('X-User-ID')
    username = request.headers.get('X-Username')
    if user_id and username:
        try:
            return int(user_id), username
        except ValueError:
            return None, None
    return None, None

def login_required(f):
    from functools import wraps
    @wraps(f)
    def decorated_function(*args, **kwargs):
        user_id, username = get_current_user_info()
        if not user_id or not username:
            return jsonify({"error": "Authentication required. Please provide X-User-ID and X-Username headers."}), 401
        # Pass user_id and username to the route function
        return f(user_id, username, *args, **kwargs)
    return decorated_function

# --- Peca Endpoints (with save_data calls) ---
@app.before_request
def log_request():
    print(f"Recebido: {request.method} {request.path}")
    
@app.route('/status', methods=['GET'])
def get_status():
    print("⚠️  /status foi acessado!")
    return jsonify({"status": "ok"})
    
def register_user(username, password):
    global next_user_id

    if not username or not password:
        print("Username and password are required")
        return

    if any(u['username'] == username for u in usuarios_db):
        print("Username already exists")
        return

    hashed_password = generate_password_hash(password)
    new_user = {
        "id": next_user_id,
        "username": username,
        "password_hash": hashed_password
    }
    usuarios_db.append(new_user)
    next_user_id += 1
    save_data()
    print("User "+username+" registered successfully")

@app.route('/login', methods=['POST'])
def login_user():
    data = request.json
    username = data.get('username')
    password = data.get('password')

    user = next((u for u in usuarios_db if u['username'] == username), None)
    if user and check_password_hash(user['password_hash'], password):
        # In a real app, you'd generate a secure session token or JWT here
        # For simplicity, we'll just return success and user info for this conceptual example
        log_action(user['id'], user['username'], "USER_LOGIN")
        return jsonify({"message": "Login successful", "user_id": user['id'], "username": user['username']}), 200
    else:
        return jsonify({"error": "Invalid credentials"}), 401

@app.route('/pecas', methods=['GET'])
def get_pecas():
    return jsonify(pecas_db), 200

@app.route('/peca', methods=['POST'])
@login_required
def add_peca(user_id, username):
    global next_peca_id
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Request must contain JSON data"}), 400

        new_peca = {
            "id": next_peca_id,
            "nome": data.get("nome", "Nova Peça"),
            "tipo": data.get("tipo", "Desconhecido"),
            "estado": data.get("estado", "Desconhecido"),
            "origem": data.get("origem", None),
            "descricao": data.get("descricao", ""),
            "imagem_path": data.get("imagem_path", "")
        }
        pecas_db.append(new_peca)
        next_peca_id += 1
        save_data() 
        log_action(user_id, username, "ADD_PECA", new_peca['id'], new_peca)
        return jsonify(new_peca), 201
    except Exception as e:
        app.logger.error(f"Error adding peca: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/peca/<int:peca_id>', methods=['PUT'])
@login_required
def update_peca(user_id, username, peca_id):
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Request must contain JSON data"}), 400

        for i, peca in enumerate(pecas_db):
            if peca['id'] == peca_id:
                old_peca_data = peca.copy()
                pecas_db[i]['nome'] = data.get('nome', peca['nome'])
                pecas_db[i]['tipo'] = data.get('tipo', peca['tipo'])
                pecas_db[i]['estado'] = data.get('estado', peca['estado'])
                pecas_db[i]['origem'] = data.get('origem', peca['origem'])
                pecas_db[i]['descricao'] = data.get('descricao', peca['descricao'])
                pecas_db[i]['imagem_path'] = data.get('imagem_path', peca['imagem_path'])
                save_data() # Save data after updating
                log_action(user_id, username, "UPDATE_PECA", peca_id, {"old_data": old_peca_data, "new_data": pecas_db[i]})
                return jsonify(pecas_db[i]), 200
        return jsonify({"error": "Peça not found"}), 404
    except Exception as e:
        app.logger.error(f"Error updating peca {peca_id}: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/peca/<int:peca_id>', methods=['DELETE'])
@login_required
def delete_peca(user_id, username, peca_id):
    global pecas_db
    peca_to_delete = None
    for peca in pecas_db:
        if peca['id'] == peca_id:
            peca_to_delete = peca
            break

    if peca_to_delete:
        if peca_to_delete.get('estado') == 'pronto':
            pecas_db = [p for p in pecas_db if p['id'] != peca_id]
            save_data() # Save data after deleting
            log_action(user_id, username, "DELETE_PECA", peca_id, {"deleted_peca": peca_to_delete})
            return jsonify({"message": f"Peça with ID {peca_id} deleted successfully"}), 200
        else:
            return jsonify({"error": f"Peça with ID {peca_id} cannot be deleted. Its state is '{peca_to_delete.get('estado')}', but must be 'pronto'."}), 400
    return jsonify({"error": "Peça not found"}), 404

# --- PC Endpoints (with save_data calls) ---

@app.route('/computadores', methods=['GET'])
def get_computadores():
    return jsonify(computadores_db), 200

@app.route('/computador', methods=['POST'])
@login_required
def add_computador(user_id, username):
    global next_pc_id
    try:
        data = request.json
        app.logger.info(f"Requisição POST recebida em /computador. Dados: {data}")
        if not data:
            app.logger.warning("Dados JSON vazios na requisição POST /computador.")
            return jsonify({"error": "Request must contain JSON data"}), 400

        new_pc = {
            "id": next_pc_id,
            "nome": data.get("nome", "Novo Computador"),
            "tipo": data.get("tipo", "Desconhecido"),
            "origem": data.get("origem", "Desconhecido"),
            "estado": data.get("estado", "Desconhecido"),
            "pecas": data.get("pecas", []),
            "descricao": data.get("descricao", "")
        }
        computadores_db.append(new_pc)
        next_pc_id += 1
        save_data() # Save data after adding
        log_action(user_id, username, "ADD_COMPUTADOR", new_pc['id'], new_pc)
        app.logger.info(f"Novo PC adicionado: {new_pc}")
        return jsonify(new_pc), 201
    except Exception as e:
        app.logger.error(f"Erro ao adicionar computador: {e}", exc_info=True)
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/computador/<int:pc_id>', methods=['PUT'])
@login_required
def update_computador(user_id, username, pc_id):
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Request must contain JSON data"}), 400

        for i, pc in enumerate(computadores_db):
            if pc['id'] == pc_id:
                old_pc_data = pc.copy()
                computadores_db[i]['nome'] = data.get('nome', pc['nome'])
                computadores_db[i]['origem'] = data.get('origem', pc['origem'])
                computadores_db[i]['tipo'] = data.get('tipo', pc['tipo'])
                computadores_db[i]['estado'] = data.get('estado', pc['estado'])
                computadores_db[i]['pecas'] = data.get('pecas', pc['pecas'])
                computadores_db[i]['descricao'] = data.get('descricao', pc['descricao'])
                save_data()
                log_action(user_id, username, "UPDATE_COMPUTADOR", pc_id, {"old_data": old_pc_data, "new_data": computadores_db[i]})
                return jsonify(computadores_db[i]), 200
        return jsonify({"error": "Computador not found"}), 404
    except Exception as e:
        app.logger.error(f"Error updating computador {pc_id}: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/computador/<int:pc_id>', methods=['DELETE'])
@login_required
def delete_computador(user_id, username, pc_id):
    global computadores_db, historico_pcs_excluidos_db

    pc_to_delete = None
    for pc in computadores_db:
        if pc['id'] == pc_id:
            pc_to_delete = pc
            break

    if pc_to_delete:
        if pc_to_delete.get('estado') == 'pronto':
            historico_pcs_excluidos_db.append(pc_to_delete.copy())
            computadores_db = [pc for pc in computadores_db if pc['id'] != pc_id]
            save_data() # Save data after deleting
            log_action(user_id, username, "DELETE_COMPUTADOR", pc_id, {"deleted_computador": pc_to_delete})
            print(f"PC with ID {pc_id} moved to history.")
            return jsonify({"message": f"Computador with ID {pc_id} deleted and added to history"}), 200
        else:
            return jsonify({"error": f"Computador with ID {pc_id} cannot be deleted. Its state is '{pc_to_delete.get('estado')}', but must be 'pronto'."}), 400
    return jsonify({"error": "Computador not found"}), 404

@app.route('/audit_log', methods=['GET'])
@login_required
def get_audit_log(user_id, username):
    return jsonify(audit_log_db), 200

@app.route('/')
def index():
    return render_template('index.html')

# --- Server Terminal Menu for History ---
def run_server_menu():
    while True:
        print("\n--- Menu do Servidor (Histórico de PCs Excluídos) ---")
        print("1. Ver Histórico de PCs Excluídos")
        print("2. Limpar Histórico de PCs Excluídos")
        print("3. Criar Usuario")
        choice = input("Escolha uma opção: ")
        global historico_pcs_excluidos_db 
        if choice == '1':
            if historico_pcs_excluidos_db:
                print("\n--- Histórico de PCs Excluídos ---")
                for pc in historico_pcs_excluidos_db:
                    print(f"ID: {pc['id']}, Nome: {pc['nome']}, Tipo: {pc['tipo']}, Origem: {pc['origem']}, Estado: {pc['estado']}")
            else:
                print("O histórico de PCs excluídos está vazio.")
        elif choice == '2':
            historico_pcs_excluidos_db = []
            save_data() # Save data after clearing history
            print("Histórico de PCs excluídos limpo com sucesso.")
        elif choice == '3':
            user = input("Nome de usuario: ")
            seed = input("senha de usuario: ")
            register_user(user,seed)
        else:
            print("Opção inválida. Tente novamente.")

if __name__ == '__main__':
    porta = int(input("Escolha porta: "))
    load_data() # Load data when the application starts
    menu_thread = threading.Thread(target=run_server_menu)
    menu_thread.daemon = True
    menu_thread.start()

    app.run(debug=True, port=porta, use_reloader=False, host='0.0.0.0')
