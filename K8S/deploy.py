import subprocess
import time
import os

# Path to folder with K8S manifests
K8S_DIR = r"C:\Users\solon\source\repos\icykcypher\spyserv-backend\K8S"

# Step 1: PostgreSQL
postgres_files = [
    "postgres-configmap.yaml",
    "postgres-secret.yaml",
    "postgres-pv.yaml",
    "postgres-pvc.yaml",
    "postgres-service.yaml",
    "postgres-statefulset.yaml",
]

# Step 2: RabbitMQ
rabbitmq_file = "rabbitmq-delp.yaml"

# Step 3: Ingress
ingress_file = "ingress-nginx.yaml"

# Step 4: Rest of services (except already-applied)
exclude_files = set(postgres_files + [rabbitmq_file, ingress_file])
other_services = [
    f for f in os.listdir(K8S_DIR)
    if f.endswith(".yaml") and f not in exclude_files
]

def apply_kubectl(file_path):
    print(f"\n🚀 Applying {file_path}...")
    result = subprocess.run(["kubectl", "apply", "-f", file_path], capture_output=True, text=True)
    if result.returncode == 0:
        print(f"✅ Applied: {file_path}")
    else:
        print(f"❌ Error applying {file_path}:\n{result.stderr}")

def wait(seconds):
    print(f"⏳ Waiting {seconds} seconds for stabilization...")
    time.sleep(seconds)

def apply_sequence():
    print("=== Step 1: Applying PostgreSQL ===")
    for f in postgres_files:
        apply_kubectl(os.path.join(K8S_DIR, f))
    wait(10)

    print("=== Step 2: Applying RabbitMQ ===")
    apply_kubectl(os.path.join(K8S_DIR, rabbitmq_file))
    wait(5)

    print("=== Step 3: Applying Ingress ===")
    apply_kubectl(os.path.join(K8S_DIR, ingress_file))
    wait(5)

    print("=== Step 4: Applying Other Services ===")
    for f in sorted(other_services):  # sorted for determinism
        apply_kubectl(os.path.join(K8S_DIR, f))

    print("\n✅ Deployment sequence completed.")

if __name__ == "__main__":
    apply_sequence()
