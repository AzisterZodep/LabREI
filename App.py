from flask import Flask, request, jsonify, render_template
from flask_cors import CORS
import threading
import json
import os # Import os module to check for file existence

app = Flask(__name__)
CORS(app)

# Define file paths for persistence
PECAS_FILE = 'pecas.json'
COMPUTADORES_FILE = 'computadores.json'
HISTORICO_FILE = 'historico.json'

pecas_db = []
computadores_db = []
historico_pcs_excluidos_db = []

next_peca_id = 1
next_pc_id = 1

# --- Persistence Functions ---
def load_data():
    global pecas_db, computadores_db, historico_pcs_excluidos_db, next_peca_id, next_pc_id
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
    print("Dados carregados com sucesso!")

def save_data():
    with open(PECAS_FILE, 'w') as f:
        json.dump(pecas_db, f, indent=4)
    with open(COMPUTADORES_FILE, 'w') as f:
        json.dump(computadores_db, f, indent=4)
    with open(HISTORICO_FILE, 'w') as f:
        json.dump(historico_pcs_excluidos_db, f, indent=4)
    print("Dados salvos com sucesso!")

# --- Peca Endpoints (with save_data calls) ---
@app.before_request
def log_request():
    print(f"Recebido: {request.method} {request.path}")
    
@app.route('/status', methods=['GET'])
def get_status():
    print("⚠️  /status foi acessado!")
    return jsonify({"status": "ok"})

@app.route('/pecas', methods=['GET'])
def get_pecas():
    return jsonify(pecas_db), 200

@app.route('/peca', methods=['POST'])
def add_peca():
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
        save_data() # Save data after adding
        return jsonify(new_peca), 201
    except Exception as e:
        app.logger.error(f"Error adding peca: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/peca/<int:peca_id>', methods=['PUT'])
def update_peca(peca_id):
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Request must contain JSON data"}), 400

        for i, peca in enumerate(pecas_db):
            if peca['id'] == peca_id:
                pecas_db[i]['nome'] = data.get('nome', peca['nome'])
                pecas_db[i]['tipo'] = data.get('tipo', peca['tipo'])
                pecas_db[i]['estado'] = data.get('estado', peca['estado'])
                pecas_db[i]['origem'] = data.get('origem', peca['origem'])
                pecas_db[i]['descricao'] = data.get('descricao', peca['descricao'])
                pecas_db[i]['imagem_path'] = data.get('imagem_path', peca['imagem_path'])
                save_data() # Save data after updating
                return jsonify(pecas_db[i]), 200
        return jsonify({"error": "Peça not found"}), 404
    except Exception as e:
        app.logger.error(f"Error updating peca {peca_id}: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/peca/<int:peca_id>', methods=['DELETE'])
def delete_peca(peca_id):
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
            return jsonify({"message": f"Peça with ID {peca_id} deleted successfully"}), 200
        else:
            return jsonify({"error": f"Peça with ID {peca_id} cannot be deleted. Its state is '{peca_to_delete.get('estado')}', but must be 'pronto'."}), 400
    return jsonify({"error": "Peça not found"}), 404

# --- PC Endpoints (with save_data calls) ---

@app.route('/computadores', methods=['GET'])
def get_computadores():
    return jsonify(computadores_db), 200

@app.route('/computador', methods=['POST'])
def add_computador():
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
        app.logger.info(f"Novo PC adicionado: {new_pc}")
        return jsonify(new_pc), 201
    except Exception as e:
        app.logger.error(f"Erro ao adicionar computador: {e}", exc_info=True)
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/computador/<int:pc_id>', methods=['PUT'])
def update_computador(pc_id):
    try:
        data = request.json
        if not data:
            return jsonify({"error": "Request must contain JSON data"}), 400

        for i, pc in enumerate(computadores_db):
            if pc['id'] == pc_id:
                computadores_db[i]['nome'] = data.get('nome', pc['nome'])
                computadores_db[i]['origem'] = data.get('origem', pc['origem'])
                computadores_db[i]['tipo'] = data.get('tipo', pc['tipo'])
                computadores_db[i]['estado'] = data.get('estado', pc['estado'])
                computadores_db[i]['pecas'] = data.get('pecas', pc['pecas'])
                computadores_db[i]['descricao'] = data.get('descricao', pc['descricao'])
                save_data() # Save data after updating
                return jsonify(computadores_db[i]), 200
        return jsonify({"error": "Computador not found"}), 404
    except Exception as e:
        app.logger.error(f"Error updating computador {pc_id}: {e}")
        return jsonify({"error": "Invalid JSON or missing data", "details": str(e)}), 400

@app.route('/computador/<int:pc_id>', methods=['DELETE'])
def delete_computador(pc_id):
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
            print(f"PC with ID {pc_id} moved to history.")
            return jsonify({"message": f"Computador with ID {pc_id} deleted and added to history"}), 200
        else:
            return jsonify({"error": f"Computador with ID {pc_id} cannot be deleted. Its state is '{pc_to_delete.get('estado')}', but must be 'pronto'."}), 400
    return jsonify({"error": "Computador not found"}), 404

@app.route('/')
def index():
    return render_template('index.html')

# --- Server Terminal Menu for History ---
def run_server_menu():
    while True:
        print("\n--- Menu do Servidor (Histórico de PCs Excluídos) ---")
        print("1. Ver Histórico de PCs Excluídos")
        print("2. Limpar Histórico de PCs Excluídos")
        print("3. Sair do Menu (o servidor web continuará rodando)")
        choice = input("Escolha uma opção: ")

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
            print("Saindo do menu. O servidor web continua ativo.")
            break
        else:
            print("Opção inválida. Tente novamente.")

if __name__ == '__main__':
    load_data() # Load data when the application starts
    menu_thread = threading.Thread(target=run_server_menu)
    menu_thread.daemon = True
    menu_thread.start()

    app.run(debug=True, port=5000, use_reloader=False, host='0.0.0.0')
