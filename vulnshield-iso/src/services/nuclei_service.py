import subprocess
import json
import logging
from typing import List, Dict, Any
from src.services.scanner_base import BaseScanner

logger = logging.getLogger(__name__)

class NucleiService(BaseScanner):
    def __init__(self, binary_path: str = 'nuclei'):
        self.binary_path = binary_path

    async def scan(self, target: str, **kwargs) -> List[Dict[str, Any]]:
        logger.info(f'Starting Nuclei scan on target: {target}')
        try:
            scan_tags = kwargs.get('scan_tags') or []
            timeout = int(kwargs.get('timeout', 600))
            cmd = [self.binary_path, '-jsonl', '-target', target]
            if scan_tags:
                cmd.extend(['-tags', ','.join(scan_tags)])

            result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
            
            if result.returncode != 0:
                logger.error(f'Nuclei failed: {result.stderr}')
                return []
            
            findings = []
            for line in result.stdout.splitlines():
                if line.strip():
                    payload = json.loads(line)
                    payload['scanner-command'] = cmd
                    findings.append(payload)
            
            return findings
        except Exception as e:
            logger.error(f'Nuclei error: {str(e)}')
            return []
